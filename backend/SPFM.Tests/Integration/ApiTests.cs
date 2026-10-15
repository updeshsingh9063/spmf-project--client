using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace SPFM.Tests.Integration;

/// <summary>
/// Drives the API over HTTP, against the real database.
///
/// Requirements §32 asks for API testing of authentication, project creation,
/// retrieval, update and the calculation endpoints. These run the whole pipeline —
/// routing, authorization, model binding, serialization, EF, PostgreSQL — so they
/// catch what unit tests and repository tests cannot: a wrong status code, a broken
/// ETag, an enum serialized as a number, a validation error with the wrong shape.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Integration")]
public class ApiTests(DatabaseFixture db) : IAsyncLifetime
{
    private SpfmApiFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task InitializeAsync()
    {
        Assert.True(db.Available, db.SkipReason ?? "No PostgreSQL server reachable.");
        _factory = new SpfmApiFactory(db.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static object NewProject(string name = "Warehouse fit-out", string? number = null) => new
    {
        number,
        name,
        customerName = "Bayview Logistics",
        siteAddress = "12 Dock Road",
        status = "quote",
        lines = new[] { new { description = "Steel security door", quantity = 4, unitCost = 820, unitPrice = 1350 } },
        variations = 0,
        labourHours = 32,
        labourRate = 55,
        subcontractorCost = 0,
        otherDirectCost = 0,
        overheadPercent = 12,
    };

    private async Task<JsonElement> CreateAsync(object? body = null)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/projects", body ?? NewProject());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    // ---- Authentication and authorization (§21) -------------------------------

    [Fact]
    public async Task Health_is_reachable_without_signing_in()
    {
        _factory.SignedIn = false;
        var response = await _client.GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("GET", "/api/v1/projects")]
    [InlineData("GET", "/api/v1/users/me")]
    [InlineData("POST", "/api/v1/projects")]
    public async Task Project_endpoints_refuse_an_anonymous_caller(string method, string path)
    {
        _factory.SignedIn = false;
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_without_the_required_scope_is_refused()
    {
        _factory.HasScope = false;
        var response = await _client.GetAsync("/api/v1/projects");
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_malformed_bearer_token_is_refused_by_the_real_pipeline()
    {
        // Deliberately not using the stub: this exercises the genuine Entra handler.
        using var realFactory = new WebApplicationFactoryWithRealAuth(db.ConnectionString);
        using var client = realFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/v1/projects");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldContain("invalid_token");
    }

    [Fact]
    public async Task Me_returns_the_signed_in_user_and_nothing_else()
    {
        var me = await _client.GetFromJsonAsync<JsonElement>("/api/v1/users/me", Json);

        me.GetProperty("name").GetString().ShouldBe("Samuel Fish");
        me.GetProperty("email").GetString().ShouldBe("samuel@shieldsecuritydoors.example");

        // §22: the full claim set must not be exposed to the browser.
        me.TryGetProperty("claims", out _).ShouldBeFalse();
        me.TryGetProperty("tid", out _).ShouldBeFalse();
    }

    // ---- Create, read (§7, §16) -----------------------------------------------

    [Fact]
    public async Task A_project_can_be_created_and_read_back()
    {
        var created = await CreateAsync();
        var id = created.GetProperty("id").GetString();

        var fetched = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{id}", Json);

        fetched.GetProperty("name").GetString().ShouldBe("Warehouse fit-out");
        fetched.GetProperty("customerName").GetString().ShouldBe("Bayview Logistics");
        fetched.GetProperty("lines").GetArrayLength().ShouldBe(1);
        fetched.GetProperty("labourHours").GetDecimal().ShouldBe(32m);
    }

    [Fact]
    public async Task Status_travels_as_a_word_not_a_number()
    {
        // A numeric enum is unreadable and silently changes meaning if a value is
        // ever inserted into the enum.
        var created = await CreateAsync();
        created.GetProperty("status").ValueKind.ShouldBe(JsonValueKind.String);
        created.GetProperty("status").GetString().ShouldBe("quote");
    }

    [Fact]
    public async Task A_missing_project_gives_a_problem_document_not_a_bare_404()
    {
        var response = await _client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("title").GetString().ShouldBe("Project not found");
    }

    [Fact]
    public async Task Invalid_input_is_reported_against_its_fields()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/projects", new
        {
            name = "",
            customerName = "",
            lines = Array.Empty<object>(),
            overheadPercent = 150,
            labourHours = 0, labourRate = 0, subcontractorCost = 0, otherDirectCost = 0, variations = 0,
            siteAddress = "", status = "quote",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var errors = problem.GetProperty("errors");

        // §10: keyed by field so the UI can place each message on its input.
        errors.TryGetProperty("name", out _).ShouldBeTrue();
        errors.TryGetProperty("customerName", out _).ShouldBeTrue();
        errors.TryGetProperty("overheadPercent", out _).ShouldBeTrue();

        errors.GetProperty("name")[0].GetString().ShouldBe("Enter a project name.");
    }

    [Fact]
    public async Task A_duplicate_project_number_is_a_conflict_with_an_explanation()
    {
        await CreateAsync(NewProject(number: "API-DUP-001"));
        var response = await _client.PostAsJsonAsync("/api/v1/projects", NewProject(number: "API-DUP-001"));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("detail").GetString()!.ShouldContain("API-DUP-001");
    }

    // ---- Update and concurrency (§29) -----------------------------------------

    [Fact]
    public async Task Saving_without_the_version_you_loaded_is_refused()
    {
        var created = await CreateAsync();
        var id = created.GetProperty("id").GetString();

        // No If-Match: the caller cannot prove which version it is editing.
        var response = await _client.PutAsJsonAsync($"/api/v1/projects/{id}", NewProject("Renamed"));

        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionRequired);
    }

    [Fact]
    public async Task A_project_can_be_edited_with_the_version_it_was_loaded_at()
    {
        var created = await CreateAsync();
        var id = created.GetProperty("id").GetString();
        var etag = created.GetProperty("etag").GetString();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/projects/{id}")
        {
            Content = JsonContent.Create(NewProject("Warehouse fit-out stage 2")),
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        updated.GetProperty("name").GetString().ShouldBe("Warehouse fit-out stage 2");

        // The version must move on, or the next save would use a stale one.
        updated.GetProperty("etag").GetString().ShouldNotBe(etag);
    }

    [Fact]
    public async Task A_second_person_saving_the_same_version_is_told_so()
    {
        var created = await CreateAsync();
        var id = created.GetProperty("id").GetString();
        var etag = created.GetProperty("etag").GetString();

        async Task<HttpResponseMessage> SaveAsync(string name)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/projects/{id}")
            {
                Content = JsonContent.Create(NewProject(name)),
            };
            request.Headers.TryAddWithoutValidation("If-Match", etag);
            return await _client.SendAsync(request);
        }

        (await SaveAsync("First person wins")).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Someone else edits from the same starting point.
        _factory.ObjectId = Guid.NewGuid().ToString();
        _factory.DisplayName = "Another user";

        var second = await SaveAsync("Second person should be stopped");
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var problem = await second.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("detail").GetString()!.ShouldContain("changed by someone else");

        // The first save must survive untouched.
        var current = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{id}", Json);
        current.GetProperty("name").GetString().ShouldBe("First person wins");
    }

    // ---- Calculate (§11, §16) --------------------------------------------------

    [Fact]
    public async Task Calculating_returns_figures_and_marks_them_current()
    {
        var created = await CreateAsync();
        var id = created.GetProperty("id").GetString();

        created.GetProperty("calculationState").GetString().ShouldBe("never");

        var response = await _client.PostAsync($"/api/v1/projects/{id}/calculate", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var results = body.GetProperty("results");

        results.GetProperty("revenue").GetDecimal().ShouldBe(5400m);      // 4 x 1350
        results.GetProperty("labourCost").GetDecimal().ShouldBe(1760m);   // 32 x 55

        body.GetProperty("calculationState").GetString().ShouldBe("current");

        // The engine version must be visible: results from placeholder formulas have
        // to be identifiable as such.
        body.GetProperty("engineVersion").GetString().ShouldStartWith("placeholder");
    }

    [Fact]
    public async Task Editing_the_inputs_makes_saved_results_outdated()
    {
        var created = await CreateAsync();
        var id = created.GetProperty("id").GetString();
        var etag = created.GetProperty("etag").GetString();

        await _client.PostAsync($"/api/v1/projects/{id}/calculate", null);

        var afterCalc = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{id}", Json);
        afterCalc.GetProperty("calculationState").GetString().ShouldBe("current");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/projects/{id}")
        {
            Content = JsonContent.Create(new
            {
                name = "Warehouse fit-out",
                customerName = "Bayview Logistics",
                siteAddress = "12 Dock Road",
                status = "quote",
                lines = new[] { new { description = "Steel security door", quantity = 4, unitCost = 820, unitPrice = 1350 } },
                variations = 0, labourHours = 48, labourRate = 55,
                subcontractorCost = 0, otherDirectCost = 0, overheadPercent = 12,
            }),
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        (await _client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // §16: stale figures must never be presented as current.
        var afterEdit = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{id}", Json);
        afterEdit.GetProperty("calculationState").GetString().ShouldBe("outdated");
    }

    // ---- Search, paging, history (§7, §17, §27) --------------------------------

    [Fact]
    public async Task Projects_can_be_found_by_part_of_their_name()
    {
        await CreateAsync(NewProject("Strongroom door replacement", "API-S-1"));
        await CreateAsync(NewProject("Loading bay shutters", "API-S-2"));

        var page = await _client.GetFromJsonAsync<JsonElement>("/api/v1/projects?q=strongroom", Json);
        var names = page.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("name").GetString()).ToList();

        names.ShouldContain("Strongroom door replacement");
        names.ShouldNotContain("Loading bay shutters");
    }

    [Fact]
    public async Task The_project_list_pages_without_repeating_or_losing_rows()
    {
        const int total = 12;
        for (var i = 0; i < total; i++)
            await CreateAsync(NewProject($"Paged API project {i:D2}", $"API-P-{i:D3}"));

        var seen = new List<string>();
        string? cursor = null;
        var pages = 0;

        do
        {
            var url = $"/api/v1/projects?q=Paged API project&limit=5" +
                      (cursor is null ? "" : $"&cursor={Uri.EscapeDataString(cursor)}");
            var page = await _client.GetFromJsonAsync<JsonElement>(url, Json);

            seen.AddRange(page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetString()!));
            cursor = page.GetProperty("nextCursor").GetString();
            pages++;
        } while (cursor is not null && pages < 10);

        seen.Count.ShouldBe(total);
        seen.Distinct().Count().ShouldBe(total);
    }

    [Fact]
    public async Task A_corrupt_cursor_restarts_the_list_rather_than_failing()
    {
        await CreateAsync(NewProject("Cursor test", "API-C-1"));

        // Stale bookmarks happen; they must not produce a 500.
        var response = await _client.GetAsync("/api/v1/projects?cursor=not-a-real-cursor");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Change_history_records_who_changed_what(
    )
    {
        var created = await CreateAsync();
        var id = created.GetProperty("id").GetString();
        var etag = created.GetProperty("etag").GetString();

        _factory.DisplayName = "Updesh";
        _factory.ObjectId = Guid.NewGuid().ToString();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/projects/{id}")
        {
            Content = JsonContent.Create(new
            {
                name = "Warehouse fit-out",
                customerName = "Bayview Logistics",
                siteAddress = "12 Dock Road",
                status = "quote",
                lines = new[] { new { description = "Steel security door", quantity = 4, unitCost = 820, unitPrice = 1350 } },
                variations = 0, labourHours = 40, labourRate = 55,
                subcontractorCost = 0, otherDirectCost = 0, overheadPercent = 12,
            }),
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        await _client.SendAsync(request);

        var history = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{id}/history", Json);
        var items = history.GetProperty("items").EnumerateArray().ToList();

        items.Count.ShouldBe(2);  // created, then updated

        var update = items.First(i => i.GetProperty("action").GetString() == "updated");
        update.GetProperty("by").GetString().ShouldBe("Updesh");

        var change = update.GetProperty("changes").EnumerateArray().Single();
        change.GetProperty("field").GetString().ShouldBe("Labour hours");
        change.GetProperty("from").GetString().ShouldBe("32");
        change.GetProperty("to").GetString().ShouldBe("40");
    }
}

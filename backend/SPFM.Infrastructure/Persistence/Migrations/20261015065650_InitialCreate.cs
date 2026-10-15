using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPFM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    qbo_customer_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entra_object_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    variations = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    labour_hours = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    labour_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    subcontractor_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    other_direct_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    overhead_percent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    extra_inputs_json = table.Column<string>(type: "jsonb", nullable: true),
                    inputs_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    qbo_project_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    qbo_last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projects_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_projects_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "calculation_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    inputs_json = table.Column<string>(type: "jsonb", nullable: false),
                    results_json = table.Column<string>(type: "jsonb", nullable: false),
                    inputs_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    engine_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    calculated_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    calculated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calculation_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_calculation_runs_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_calculation_runs_users_calculated_by_id",
                        column: x => x.calculated_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "change_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_sets", x => x.id);
                    table.ForeignKey(
                        name: "fk_change_sets_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_change_sets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    qbo_item_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_lines_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    old_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    new_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_field_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_field_changes_change_sets_change_set_id",
                        column: x => x.change_set_id,
                        principalTable: "change_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_calculation_runs_calculated_by_id",
                table: "calculation_runs",
                column: "calculated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_calculation_runs_project_id_calculated_at",
                table: "calculation_runs",
                columns: new[] { "project_id", "calculated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_calculation_runs_project_id_version",
                table: "calculation_runs",
                columns: new[] { "project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_change_sets_project_id_at",
                table: "change_sets",
                columns: new[] { "project_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_change_sets_user_id",
                table: "change_sets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_name",
                table: "customers",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_qbo_customer_id",
                table: "customers",
                column: "qbo_customer_id",
                filter: "qbo_customer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_field_changes_change_set_id",
                table: "field_changes",
                column: "change_set_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_lines_project_id_ordinal",
                table: "project_lines",
                columns: new[] { "project_id", "ordinal" });

            migrationBuilder.CreateIndex(
                name: "ix_projects_created_by_id",
                table: "projects",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_customer_id",
                table: "projects",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_name",
                table: "projects",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_projects_number",
                table: "projects",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_number_trgm",
                table: "projects",
                column: "number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_projects_status_updated_at",
                table: "projects",
                columns: new[] { "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_projects_updated_at_id",
                table: "projects",
                columns: new[] { "updated_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_projects_updated_by_id",
                table: "projects",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_entra_object_id",
                table: "users",
                column: "entra_object_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calculation_runs");

            migrationBuilder.DropTable(
                name: "field_changes");

            migrationBuilder.DropTable(
                name: "project_lines");

            migrationBuilder.DropTable(
                name: "change_sets");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

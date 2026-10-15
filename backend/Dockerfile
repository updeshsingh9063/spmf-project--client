# SPFM API.
#
# Built from the repository root:
#   docker build -f backend/Dockerfile -t spfm-api .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first, as its own layer: dependencies change far less often than code,
# so edits do not re-download the world.
COPY SPFM.sln ./
COPY backend/SPFM.Domain/SPFM.Domain.csproj          backend/SPFM.Domain/
COPY backend/SPFM.Application/SPFM.Application.csproj backend/SPFM.Application/
COPY backend/SPFM.Infrastructure/SPFM.Infrastructure.csproj backend/SPFM.Infrastructure/
COPY backend/SPFM.Api/SPFM.Api.csproj                backend/SPFM.Api/
COPY backend/SPFM.Tests/SPFM.Tests.csproj            backend/SPFM.Tests/
RUN dotnet restore backend/SPFM.Api/SPFM.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/SPFM.Api/SPFM.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root. A container that does not need root should not run as it.
RUN adduser --system --uid 1001 --group spfm
USER spfm

COPY --from=build --chown=spfm:spfm /app ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Liveness only. Readiness (/health/ready) includes the database, and a database
# blip should not make the orchestrator kill a healthy process.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SPFM.Api.dll"]

# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props SubscriptionTracker.slnx ./
COPY src/Core/SubscriptionTracker.Domain/SubscriptionTracker.Domain.csproj src/Core/SubscriptionTracker.Domain/
COPY src/Core/SubscriptionTracker.Application/SubscriptionTracker.Application.csproj src/Core/SubscriptionTracker.Application/
COPY src/Infrastructure/SubscriptionTracker.Infrastructure/SubscriptionTracker.Infrastructure.csproj src/Infrastructure/SubscriptionTracker.Infrastructure/
COPY src/Presentation/SubscriptionTracker.Api/SubscriptionTracker.Api.csproj src/Presentation/SubscriptionTracker.Api/
RUN dotnet restore src/Presentation/SubscriptionTracker.Api/SubscriptionTracker.Api.csproj

COPY src/Core/SubscriptionTracker.Domain/ src/Core/SubscriptionTracker.Domain/
COPY src/Core/SubscriptionTracker.Application/ src/Core/SubscriptionTracker.Application/
COPY src/Infrastructure/SubscriptionTracker.Infrastructure/ src/Infrastructure/SubscriptionTracker.Infrastructure/
COPY src/Presentation/SubscriptionTracker.Api/ src/Presentation/SubscriptionTracker.Api/

RUN dotnet publish src/Presentation/SubscriptionTracker.Api/SubscriptionTracker.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is needed for the HEALTHCHECK below, adduser for the non-root user setup — neither ships
# in the aspnet:10.0 base image.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl adduser \
    && rm -rf /var/lib/apt/lists/*

# No explicit --gid/--uid: the aspnet:10.0 base image already has GID/UID 1000 taken by its
# built-in "ubuntu" user, so let adduser pick the next free ids instead of colliding with it.
RUN addgroup --system appgroup \
    && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "SubscriptionTracker.Api.dll"]

# This Dockerfile is built with Root Directory = src/api as the build context
# (see docs/specs/operations/environments.md). All COPY paths below are
# relative to that context, not the repo root.
#
# An explicit Dockerfile removes Railway's build-driver ambiguity (Nixpacks vs
# Railpack have both auto-detected and misbuilt against the sibling .sln in
# this folder, which references the test project outside this build context).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ProjectTracker.Api.csproj ./
RUN dotnet restore ProjectTracker.Api.csproj

COPY . ./
RUN dotnet publish ProjectTracker.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 3000
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-3000} dotnet ProjectTracker.Api.dll"]

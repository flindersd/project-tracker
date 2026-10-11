# Initiative Tracker

Monorepo scaffold for the Initiative Tracker web app and ASP.NET Core API.

## Services

- `src/web` — React + Vite SPA
- `src/api` — ASP.NET Core modular monolith API
- `packages/contracts` — shared TypeScript contract types

## Getting started

```bash
npm install
npm run dev:web
npm run dev:api
```

## Quality checks

```bash
npm run lint
npm run test
npm run build
dotnet test src/api/ProjectTracker.Api.sln
```

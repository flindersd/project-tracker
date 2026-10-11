# Contributing

## Structure

- Frontend lives in `src/web`
- Backend lives in `src/api`
- Shared types live in `packages/contracts`

## Conventions

- TypeScript uses strict mode and named exports where practical.
- C# uses nullable reference types and async suffixes for async methods.
- Prefer small, focused modules and feature folders.
- Keep secrets out of source control; use `.env.example` as the template.

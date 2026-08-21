# GRINDR fitness app

## Run locally

```sh
dotnet run --project ConsoleApp1.csproj --urls http://localhost:5580
```

Open <http://localhost:5580>.

## API

- `GET /api/health`
- `GET /api/exercises?page=1&pageSize=24&search=bench&muscle=Chest`
- `GET /api/exercises/{id}`
- `GET /api/exercises/{id}/demo.svg`
- `POST /api/auth/login`
- `POST /api/workouts`
- `GET /api/workouts/{email}`

The exercise catalog currently contains 1,008 generated exercise records. Each record has a unique animated SVG demo endpoint. Replace `ExerciseCatalog` with a licensed ExerciseDB import when production exercise data is available.

## Deploy publicly

The included `Dockerfile` is ready for any container host such as Render, Railway, Fly.io, Azure App Service, or Google Cloud Run. Configure the host to build from this folder and expose port `8080`.

A permanent public URL requires a deployment account and production configuration. The current account/workout store is in-memory for local prototyping; use a database and secure password hashing before public launch. Keep Google, AI, file-storage, and Stripe secrets in the host's environment variables, never in the frontend.

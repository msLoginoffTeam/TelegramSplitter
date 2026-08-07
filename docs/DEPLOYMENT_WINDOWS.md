# Production deployment: Windows + Tailscale Funnel

Backend is deployed by the organization-level GitHub Actions self-hosted runner with label `splitter-prod`. The frontend is deployed separately from its own repository.

## Production topology

```text
Tailscale Funnel -> frontend (127.0.0.1:8080)
                       -> splitter-internal Docker network -> splitter-api:8080
PostgreSQL -> private default Docker network only
```

The API exposes `127.0.0.1:5050` only for local diagnostics on the Windows host. It is not reachable from the internet, and the frontend uses `http://splitter-api:8080` within Docker.

## First deployment

1. In GitHub repository Settings -> Secrets and variables -> Actions, add the repository secrets `POSTGRES_PASSWORD` and `TELEGRAM_BOT_TOKEN`. They are injected only into the production deploy job.
2. To run a deployment manually from a local PowerShell, set these two environment variables for that terminal session.
3. Run:

   ```powershell
   ./scripts/deploy-production.ps1
   ```

4. Validate API and migrations:

   ```powershell
   Invoke-WebRequest http://127.0.0.1:5050/health
   docker compose -f compose.production.yml ps
   ```

The API runs `Database.Migrate()` at startup. The first launch creates the PostgreSQL schema; later deployments apply committed EF migrations automatically.

## CD workflow

`tests.yml` runs on GitHub-hosted Ubuntu for every pull request and push to `main`. Only a successful push to `main` starts the `deploy` job on the Windows runner. The runner builds the checked-out commit locally with Docker Compose; no container registry or SSH credential is necessary.

`POSTGRES_PASSWORD` and `TELEGRAM_BOT_TOKEN` are GitHub **repository secrets**, not files in the runner workspace. GitHub masks them in workflow logs. They are still necessarily present in the running database/API containers, so access to the Windows host and Docker daemon must remain restricted.

Create one **organization-level** runner under `msLoginoffTeam` (Organization Settings -> Actions -> Runners) and give it label `splitter-prod`. Configure its Windows service under the same Windows account that runs Docker Desktop, so Docker daemon access is available. The service setup requires an elevated PowerShell.

## Backups

Before inviting real users, configure a scheduled `pg_dump` from the `db` container to a folder outside the Docker volume and ensure that folder is backed up. The named volume protects against container recreation, but not against disk loss or accidental host deletion.

$ErrorActionPreference = "Stop"

foreach ($variableName in @("POSTGRES_PASSWORD", "TELEGRAM_BOT_TOKEN")) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($variableName))) {
        throw "Missing required environment variable: $variableName"
    }
}

docker network inspect splitter-internal *> $null
if ($LASTEXITCODE -ne 0) {
    docker network create splitter-internal
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to create Docker network splitter-internal."
    }
}

docker compose -f compose.production.yml up -d --build --remove-orphans
if ($LASTEXITCODE -ne 0) {
    throw "Backend deployment failed."
}

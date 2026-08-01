# Локальная разработка backend

Compose этого репозитория поднимает только API и PostgreSQL. Mini App живёт в соседнем репозитории и запускается отдельно.

Нужен запущенный Docker-compatible runtime, например OrbStack.

## Конфигурация

Скопируйте [`.env.example`](../.env.example) в `.env` и задайте безопасный локальный пароль PostgreSQL. `.env` не коммитится и используется только Compose.

Для API, запущенного из Rider или `dotnet run`, можно скопировать [`appsettings.Local.example.json`](../BudgetSplitter.App/appsettings.Local.example.json) в `BudgetSplitter.App/appsettings.Local.json`. Этот файл не коммитится. Приоритет настроек: стандартные `appsettings*.json` → `appsettings.Local.json` → environment variables.

## Ежедневный сценарий: БД в Docker, API из IDE

```bash
docker compose up -d db
```

После этого запустите `BudgetSplitter.App` из Rider. Локальная БД доступна на `localhost:54321`; API обычно слушает `http://localhost:5028` согласно вашему `launchSettings.json`.

## Проверка контейнерной поставки API

```bash
docker compose up --build -d
```

После сборки API доступен на <http://localhost:5050>, Swagger — на <http://localhost:5050/swagger>, health check — на <http://localhost:5050/health>. Этот сценарий повторяет будущий server deployment, но пока не делает API production-secure: auth и авторизация ещё не реализованы.

## Остановка и очистка

```bash
docker compose down
```

Команда оставляет named volume БД. Для полностью чистой тестовой БД используйте только после проверки цели:

```bash
docker compose down -v
```

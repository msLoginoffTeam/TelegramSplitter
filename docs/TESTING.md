# Backend tests

```bash
dotnet test BudgetSplitterWebApi.sln
```

Для integration tests нужен запущенный Docker-compatible runtime, например OrbStack. Они не используют локальную development БД и не требуют `.env`.

## Структура

- `BudgetSplitter.App.UnitTests` — быстрые проверки чистой логики без сети и БД.
- `BudgetSplitter.App.IntegrationTests` — API через `WebApplicationFactory` и настоящий PostgreSQL 16 в Testcontainers.

`PostgreSqlFixture` запускает один временный контейнер на прогон. `IntegrationTestBase` перед каждым тестом очищает все прикладные таблицы через Respawn, сохраняя только историю EF migrations. Новый integration test должен наследоваться от него и не обращаться к локальной БД вручную.

Начальное покрытие: Telegram authentication. Следующими на этом же фундаменте добавляются tests group authorization, денежных инвариантов, транзакций и transfers.

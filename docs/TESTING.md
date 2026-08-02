# Backend tests

Для EF-команд из репозитория один раз выполни `dotnet tool restore`: версия `dotnet-ef` фиксируется в `.config/dotnet-tools.json` и не зависит от глобально установленного инструмента.

```bash
dotnet test BudgetSplitterWebApi.sln
```

Для integration tests нужен запущенный Docker-compatible runtime, например OrbStack. По умолчанию factory запускает API в environment `Tests`, поэтому используется versioned `BudgetSplitter.App/appsettings.Tests.json`, а не `.env` или `appsettings.Local.json`. Сам DbContext подключается к временному PostgreSQL из Testcontainers.

## Структура

- `BudgetSplitter.App.UnitTests` — быстрые проверки чистой логики без сети и БД.
- `BudgetSplitter.App.IntegrationTests` — API через `WebApplicationFactory` и настоящий PostgreSQL 16 в Testcontainers.

`PostgreSqlFixture` запускает один временный контейнер на прогон. `IntegrationTestBase` перед каждым тестом очищает все прикладные таблицы через Respawn, сохраняя только историю EF migrations. Новый integration test должен наследоваться от него и не обращаться к локальной БД вручную.

Покрыты Telegram authentication, self-service Users API, role presets, AND-семантика permission attribute и основные сценарии group authorization: membership, viewer/member/admin/owner, `own/any`, cross-group protection и ownership transfer. Integration suite также проверяет создание трат, shares и прямые/expense payments, включая синхронизацию `IsPaid` после update/delete, а также balances и инвариант обнуления балансов предложенными transfers. Следующим на этом же фундаменте добавляется migration заполненной БД и history.

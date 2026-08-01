# Telegram Splitter — журнал проблем и рисков

Обновлено: 2026-08-02. Статусы: `open`, `planned`, `fixed`, `won't fix`.

| ID | Приоритет | Статус | Область | Проблема / риск |
|---|---|---|---|---|
| TS-001 | P1 | fixed | Local Git | Устаревшая локальная копия backend `/Users/max/RiderProjects/TelegramSplitter` удалена; канонической остаётся `/Users/max/RiderProjects/BudgetSplitterWebApi`. |
| TS-002 | P0 | open | Security | Telegram `initData` validation и authentication добавлены для всех `/api` controller endpoints; в Development доступен отдельный dev header. Group-level authorization и отказ от user IDs в DTO ещё не сделаны, поэтому клиент всё ещё может попытаться действовать от имени другого участника. |
| TS-003 | P0 | open | Backend | Удаление expense и participant не проверяет переданный `groupId` (коммиты `3deda12`, `becaea4`), допуская cross-group mutation. |
| TS-004 | P0 | fixed | Secrets | `Program.cs` больше не выводит connection string в stdout. |
| TS-005 | P0 | open | Data | `CreateExpenseAsync` сохраняет expense до проверки shares и делает два `SaveChanges`; при ошибке остаётся неполная трата. |
| TS-006 | P0 | open | Data | Не проверяется membership payer/share users; нет unique index `(ExpenseId, UserId)`. Возможны чужие участники и дубли долей, хотя balance использует `Single`. |
| TS-007 | P0 | open | Money | DTO допускают нулевые/отрицательные суммы, `FromUser == ToUser` и пустые title. Нет валюты группы и явных правил округления. |
| TS-008 | P1 | open | Groups | Создатель группы не добавляется в `UserGroups`, поэтому детали/балансы могут его не показывать. |
| TS-009 | P1 | open | Payments | `IsPaid` ставится в `true`, но не сбрасывается при уменьшении/удалении payment или изменении share. |
| TS-010 | P1 | open | Persistence | Payment–Expense использует shadow FK, а `ExpenseShare.Payments` создаёт второй неиспользуемый shadow FK `ExpenseShareId`; delete semantics неоднозначны. |
| TS-011 | P1 | open | API | Контракт неоднороден: group context частично в path, частично в query; `userId` filter и `includeDrafts` фактически игнорируются. |
| TS-012 | P1 | open | Backend | History не реализована; `useNpAlgorithm` не влияет на расчёт; greedy transfers не гарантируют минимальное число переводов. |
| TS-013 | P1 | open | Bot | Бот логирует `BOT_TOKEN`, включает debug и использует long polling. Токен необходимо исключить из логов. |
| TS-014 | P1 | open | Bot | Сценарные состояния хранятся в глобальных `map` без mutex, TTL и persistence: race risk, утечки и потеря состояния после рестарта. |
| TS-015 | P1 | open | Bot | `GetUserUUIDbyid` передаёт message по значению и декодирует user response как `GroupResponseDto`, скрывая ошибки и создавая риск nil/panic. |
| TS-016 | P1 | planned | API clients | Репозитории остаются раздельными. Нужна автоматическая генерация Go и TypeScript/React Query clients из OpenAPI плюс CI drift check. |
| TS-017 | P1 | open | Testing | Есть reusable test foundation и 8 tests для Telegram auth: unit validation плюс integration API/PostgreSQL. Денежные операции, group authorization, migrations и transfers пока не покрыты; новый workflow запускает `dotnet test` на push/PR. |
| TS-018 | P1 | fixed | Docker | Backend Compose теперь поднимает только `db` и `api`; frontend имеет независимый Compose. Локальный основной путь — БД в Docker и API из IDE. Bot намеренно не включён. |
| TS-019 | P2 | open | Docker/CI | Backend Compose готов для контейнерной поставки, но API healthcheck, non-root runtime и CI/CD deployment policy ещё нужно определить перед VPS. |
| TS-020 | P2 | fixed | Git | `.gitignore` больше не маскирует все `appsettings.*.json`, Dockerfile и Compose-файлы; локально секретными остаются только `.env` и `appsettings.Local.json`. |
| TS-021 | P2 | open | API | Добавлен `/health`. Swagger всё ещё включён во всех environments; отсутствуют CORS policy, pagination, rate limiting и optimistic concurrency. |
| TS-022 | P2 | open | Migration | Миграция добавляет обязательный `Groups.CreatedById` с `Guid.Empty`; обновление заполненной БД может упасть по FK. |
| TS-023 | P2 | open | Users | `DisplayName` unique, хотя Telegram-имена не уникальны и меняются. Идентичность должна опираться на Telegram ID. |
| TS-024 | P2 | open | Working tree | В канонической локальной копии изменён `appsettings.Development.json`. Это пользовательское изменение: не перезаписывать, не печатать секреты, перед commit решить — оставить локальным или заменить безопасным шаблоном. |
| TS-025 | P1 | open | CI/CD | Backend и bot workflows всё ещё ориентированы на Docker Hub/VPS и self-hosted runner; bot deploy запускается после push в `main`. На период локальной разработки deployment нужно отключить или сделать только ручным через защищённый GitHub Environment. |
| TS-026 | P2 | open | Code quality | Release build имеет два compiler warning: nullable `Group.CreatedBy` и XML `param` с неверным именем в `ExpensesController`. Перед включением warning-as-error их нужно устранить. |
| TS-027 | P1 | open | Bot integration | Защищённые API endpoints теперь требуют Telegram Mini App auth. У Go-бота пока нет отдельной server-to-server identity, поэтому его API-вызовы нужно адаптировать до следующего production запуска. Не добавлять постоянный обход через подстановку user ID. |
| TS-028 | P2 | fixed | Dependencies | Миграция на .NET 10 выявила восемь CVE в транзитивном `System.Security.Cryptography.Xml` 9.0.0. Источником были лишние EF design-time пакеты в persistence-проекте; они удалены, а `dotnet-ef` закреплён в tool manifest. |
| TS-029 | P1 | fixed | CI testing | Integration tests в GitHub Actions не запускались без `appsettings.Local.json`, а .NET 10 оставлял исходную EF registration после частичной замены. Добавлены versioned `appsettings.Tests.json`, environment `Tests` и удаление `IDbContextOptionsConfiguration<AppDbContext>` перед подключением Testcontainers PostgreSQL. |

## Открытые продуктовые решения

- Одна валюта на группу или валюта на трату; для MVP рекомендуется одна валюта группы.
- Роли: кто может менять и удалять чужие операции.
- Hard delete либо audit trail + soft delete.
- Multiple payers, recurring expenses, receipts и comments не включать в первый MVP без отдельного решения.
- «Закрытие» группы лучше делать как archive/settled snapshot, а не удаление истории.

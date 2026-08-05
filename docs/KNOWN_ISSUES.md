# Telegram Splitter — журнал проблем и рисков

Обновлено: 2026-08-06. Статусы: `open`, `planned`, `fixed`, `won't fix`.

| ID | Приоритет | Статус | Область | Проблема / риск |
|---|---|---|---|---|
| TS-001 | P1 | fixed | Local Git | Устаревшая локальная копия backend `/Users/max/RiderProjects/TelegramSplitter` удалена; канонической остаётся `/Users/max/RiderProjects/BudgetSplitterWebApi`. |
| TS-002 | P0 | fixed | Security | Все group-scoped endpoints требуют membership и нужное permission. Статические правила задаются декларативно через `RequireGroupPermission`; операции `own/any` сверяют автора записи и route `groupId`. |
| TS-003 | P0 | fixed | Backend | Удаление expense и participant сверяют переданный `groupId`, поэтому cross-group mutation отклоняется. |
| TS-004 | P0 | fixed | Secrets | `Program.cs` больше не выводит connection string в stdout. |
| TS-005 | P0 | fixed | Data | `CreateExpenseAsync` валидирует доли до сохранения и сохраняет expense со всеми shares одним `SaveChanges`. |
| TS-006 | P0 | fixed | Data | Создание расходов проверяет membership payer/share users и дубли в DTO; migration `HardenMoneyIntegrity` добавляет unique index `(ExpenseId, UserId)`. |
| TS-007 | P0 | fixed | Money | Expense/payment validation и DB constraints запрещают неположительные суммы, self-payment и пустой expense title. Для MVP валюта фиксирована: рубли. |
| TS-008 | P1 | fixed | Groups | Создатель группы добавляется в `UserGroups` и получает полный набор owner permissions при создании группы. |
| TS-009 | P1 | fixed | Payments | `IsPaid` больше не хранится: оно вычисляется из платежей на чтении. Изменение доли запрещено, если она станет меньше уже учтённых платежей; удаление участника с платежами также запрещено. |
| TS-010 | P1 | fixed | Persistence | Payment–Expense использует явный nullable `ExpenseId` с каскадным удалением при удалении траты. Неиспользуемая shadow-связь payment→share удалена. |
| TS-011 | P1 | open | API | Контракт неоднороден: group context частично в path, частично в query; `userId` filter и `includeDrafts` фактически игнорируются. |
| TS-012 | P1 | open | Backend | History не реализована; `useNpAlgorithm` не влияет на расчёт; greedy transfers не гарантируют минимальное число переводов. |
| TS-013 | P1 | open | Bot | Бот логирует `BOT_TOKEN`, включает debug и использует long polling. Токен необходимо исключить из логов. |
| TS-014 | P1 | open | Bot | Сценарные состояния хранятся в глобальных `map` без mutex, TTL и persistence: race risk, утечки и потеря состояния после рестарта. |
| TS-015 | P1 | open | Bot | `GetUserUUIDbyid` передаёт message по значению и декодирует user response как `GroupResponseDto`, скрывая ошибки и создавая риск nil/panic. |
| TS-016 | P1 | planned | API clients | Репозитории остаются раздельными. Нужна автоматическая генерация Go и TypeScript/React Query clients из OpenAPI плюс CI drift check. |
| TS-017 | P1 | open | Testing | Есть reusable test foundation: Telegram auth, self-service Users API и основные group permissions покрыты unit/integration tests через API/PostgreSQL. Миграции, денежные операции и transfers пока не покрыты отдельными сценариями; новый workflow запускает `dotnet test` на push/PR. |
| TS-018 | P1 | fixed | Docker | Backend Compose теперь поднимает только `db` и `api`; frontend имеет независимый Compose. Локальный основной путь — БД в Docker и API из IDE. Bot намеренно не включён. |
| TS-019 | P2 | open | Docker/CI | Backend Compose готов для контейнерной поставки, но API healthcheck, non-root runtime и CI/CD deployment policy ещё нужно определить перед VPS. |
| TS-020 | P2 | fixed | Git | `.gitignore` больше не маскирует все `appsettings.*.json`, Dockerfile и Compose-файлы; локально секретными остаются только `.env` и `appsettings.Local.json`. |
| TS-021 | P2 | open | API | Добавлен `/health`. Swagger всё ещё включён во всех environments; отсутствуют CORS policy, pagination, rate limiting и optimistic concurrency. |
| TS-022 | P2 | open | Migration | Миграция добавляет обязательный `Groups.CreatedById` с `Guid.Empty`; обновление заполненной БД может упасть по FK. |
| TS-023 | P2 | fixed | Users | Уникальный индекс `DisplayName` удалён migration `RemoveUniqueDisplayNameConstraint`; идентичность опирается на unique Telegram ID. |
| TS-024 | P2 | open | Working tree | В канонической локальной копии изменён `appsettings.Development.json`. Это пользовательское изменение: не перезаписывать, не печатать секреты, перед commit решить — оставить локальным или заменить безопасным шаблоном. |
| TS-025 | P1 | open | CI/CD | Backend и bot workflows всё ещё ориентированы на Docker Hub/VPS и self-hosted runner; bot deploy запускается после push в `main`. На период локальной разработки deployment нужно отключить или сделать только ручным через защищённый GitHub Environment. |
| TS-026 | P2 | open | Code quality | Release build имеет два compiler warning: nullable `Group.CreatedBy` и XML `param` с неверным именем в `ExpensesController`. Перед включением warning-as-error их нужно устранить. |
| TS-027 | P1 | open | Bot integration | Защищённые API endpoints теперь требуют Telegram Mini App auth. У Go-бота пока нет отдельной server-to-server identity, поэтому его API-вызовы нужно адаптировать до следующего production запуска. Не добавлять постоянный обход через подстановку user ID. |
| TS-028 | P2 | fixed | Dependencies | Миграция на .NET 10 выявила восемь CVE в транзитивном `System.Security.Cryptography.Xml` 9.0.0. Источником были лишние EF design-time пакеты в persistence-проекте; они удалены, а `dotnet-ef` закреплён в tool manifest. |
| TS-029 | P1 | fixed | CI testing | Integration tests в GitHub Actions не запускались без `appsettings.Local.json`, а .NET 10 оставлял исходную EF registration после частичной замены. Добавлены versioned `appsettings.Tests.json`, environment `Tests` и удаление `IDbContextOptionsConfiguration<AppDbContext>` перед подключением Testcontainers PostgreSQL. |
| TS-030 | P0 | fixed | API contract | `POST /api/groups` сохранял группу, но отвечал `200 OK` при OpenAPI-контракте на `201 Created`; generated frontend client ошибочно считал успешное создание неуспешным. Endpoint возвращает `CreatedAtAction`, поведение покрыто integration test. |
| TS-031 | P1 | fixed | Invites | Добавлены group invite-ссылки с хешированием токена, сроком действия и idempotent accept; новый участник получает базовые member permissions. |
| TS-032 | P1 | open | Telegram chat sync | Bot API не умеет перечислять всех исторических участников чата. Автосоздание группы при добавлении бота потребует ограниченного набора участников (админы/события) либо отдельного MTProto-решения. |

## Открытые продуктовые решения

- Одна валюта на группу или валюта на трату; для MVP рекомендуется одна валюта группы.
- Роли: кто может менять и удалять чужие операции.
- Hard delete либо audit trail + soft delete.
- Multiple payers, recurring expenses, receipts и comments не включать в первый MVP без отдельного решения.
- «Закрытие» группы лучше делать как archive/settled snapshot, а не удаление истории.

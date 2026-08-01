# Telegram Splitter — журнал проблем и рисков

Обновлено: 2026-08-01. Статусы: `open`, `planned`, `fixed`, `won't fix`.

| ID | Приоритет | Статус | Область | Проблема / риск |
|---|---|---|---|---|
| TS-001 | P1 | fixed | Local Git | Устаревшая локальная копия backend `/Users/max/RiderProjects/TelegramSplitter` удалена; канонической остаётся `/Users/max/RiderProjects/BudgetSplitterWebApi`. |
| TS-002 | P0 | open | Security | В API нет Telegram `initData` validation, authentication и group-level authorization. Клиент может подставить чужой Telegram/user/group ID. |
| TS-003 | P0 | open | Backend | Удаление expense и participant не проверяет переданный `groupId` (коммиты `3deda12`, `becaea4`), допуская cross-group mutation. |
| TS-004 | P0 | open | Secrets | `Program.cs` выводит connection string в stdout, включая пароль БД. |
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
| TS-017 | P1 | open | Testing | Нет содержательных unit/integration tests; backend release workflow не запускает .NET tests. |
| TS-018 | P1 | open | Docker | Нет единого локального Compose для db+api+web+bot; текущий Compose/deploy зависят от внешней сети `my-network`. |
| TS-019 | P2 | open | Docker/CI | Compose `version` устарел, CI actions требуют обновления, bot image работает от root, healthchecks есть только у БД. |
| TS-020 | P2 | open | Git | `.gitignore` слишком широко игнорирует `appsettings.*.json`, `Dockerfile.*` и `docker-compose.*.yml`; безопасные шаблоны легко потерять. |
| TS-021 | P2 | open | API | Swagger включён во всех environments; нет CORS policy, health endpoints, pagination, rate limiting и optimistic concurrency. |
| TS-022 | P2 | open | Migration | Миграция добавляет обязательный `Groups.CreatedById` с `Guid.Empty`; обновление заполненной БД может упасть по FK. |
| TS-023 | P2 | open | Users | `DisplayName` unique, хотя Telegram-имена не уникальны и меняются. Идентичность должна опираться на Telegram ID. |
| TS-024 | P2 | open | Working tree | В канонической локальной копии изменён `appsettings.Development.json`. Это пользовательское изменение: не перезаписывать, не печатать секреты, перед commit решить — оставить локальным или заменить безопасным шаблоном. |
| TS-025 | P1 | open | CI/CD | Backend и bot workflows всё ещё ориентированы на Docker Hub/VPS и self-hosted runner; bot deploy запускается после push в `main`. На период локальной разработки deployment нужно отключить или сделать только ручным через защищённый GitHub Environment. |

## Открытые продуктовые решения

- Одна валюта на группу или валюта на трату; для MVP рекомендуется одна валюта группы.
- Роли: кто может менять и удалять чужие операции.
- Hard delete либо audit trail + soft delete.
- Multiple payers, recurring expenses, receipts и comments не включать в первый MVP без отдельного решения.
- «Закрытие» группы лучше делать как archive/settled snapshot, а не удаление истории.

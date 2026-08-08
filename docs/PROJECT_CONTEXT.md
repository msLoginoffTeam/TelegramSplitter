# Telegram Splitter — контекст проекта

Обновлено: 2026-08-08.

## Карта знаний

- Этот файл: архитектура, доменная модель, состояние репозиториев и план.
- `docs/KNOWN_ISSUES.md`: подтверждённые баги, риски и открытые продуктовые решения.
- `docs/TESTING.md`: запуск тестов и reusable integration-test infrastructure.
- Корневой `AGENTS.md`: обязательный указатель для следующих сессий Codex.

## Канонические репозитории

## Рабочий Git-процесс

- Codex не выполняет `git commit` и `git push` без отдельного явного разрешения пользователя.
- После правок Codex оставляет рабочий diff, проверяет сборку/форматирование и предлагает сообщение для коммита; пользователь сначала проверяет изменения и коммитит их самостоятельно.

- Backend: <https://github.com/msLoginoffTeam/TelegramSplitter>
  - активная локальная копия: `/Users/max/RiderProjects/BudgetSplitterWebApi`;
  - `main`: `c786244` (`cash adjustments and validation`), опубликован в GitHub на 2026-08-03;
  - remote использует HTTPS.
- Telegram adapter: <https://github.com/msLoginoffTeam/tg_splitter_adapter>
  - `main`: `dd8e487` на момент проверки;
  - Go 1.23.5, long polling, сгенерированный OpenAPI-клиент.
- Frontend/Mini App: <https://github.com/msLoginoffTeam/TelegramSplitterMiniApp>
  - локальная копия: `/Users/max/RiderProjects/TelegramSplitterMiniApp`;
  - public repository создан 2026-08-02, baseline context commit: `d89e6a2`.
- Устаревший локальный дубль `/Users/max/RiderProjects/TelegramSplitter` удалён пользователем.

## Техническое состояние

- ASP.NET Core / EF Core / PostgreSQL 16, target `net10.0`; SDK фиксирован в `global.json` (`10.0.101`), локальный `dotnet-ef` — в `.config/dotnet-tools.json`.
- `AppDbContextDesignTimeFactory` из persistence-проекта изолирует EF tooling от запуска API и автоматического применения migration; `dotnet ef migrations add` больше не требует локальную БД.
- На текущем этапе БД считается чистой: новые migrations не обязаны переносить или чинить исторические production-данные. Перед первым реальным production deployment это решение нужно пересмотреть.
- Backend собирается без compiler warnings.
- Есть 17 unit и 41 integration test. Integration suite использует xUnit, `WebApplicationFactory`, Testcontainers PostgreSQL и Respawn; CI запускает их на push/PR.
- Go-бот собирается (`go test ./...`), но содержательных тестов нет.
- Docker CLI и OrbStack доступны; Compose разделён между репозиториями.
- Локальные секреты остаются только в игнорируемых `appsettings.Local.json`/`.env`; не выводить и не коммитить их.

## Audit history

- История доменных изменений хранится в `AuditLogEntries`; у записи есть время, группа, Telegram-инициатор, тип/операция сущности, ключ и JSON старых/новых значений.
- Используется `AuditLogLens` `0.2.0-alpha.2` в transactional-режиме. `AppDbContext.SaveChanges` централизованно открывает retry-safe transaction; данные и audit entry фиксируются или откатываются вместе.
- Автоматически аудируются `Group`, `Expense`, `ExpenseShare`, `Payment`. `User` намеренно исключён: профиль синхронизируется при открытии Mini App и не должен засорять историю. Не хранится и `GroupInvite.TokenHash`.
- Изменения `UserGroups` отражаются штатным `Collection`-enricher как поле `Group.Members`, включая синтетическое событие группы при добавлении/удалении участника. Изменения набора прав и создание invite фиксируются понятными ручными событиями; в invite-логе хранится только последние четыре символа токена, никогда не полный токен/ссылка.
- `OperationReferenceAuditEnricher` добавляет к денежным событиям неизменяемый контекст связей: название траты, участника её доли, плательщика/автора траты, отправителя и получателя платежа. Это нужно для human-readable истории даже если изменилось только поле `Amount`.
- У `AuditLogEntries.GroupId` нет внешнего ключа намеренно: журнал остаётся после удаления группы. `AsNoTracking` на read-only запросах сохраняется — он не мешает аудиту записей через `SaveChanges`.
- WIP 2026-08-08: после commit `da98b22` локально добавлена история аудита. В backend рабочем diff есть read-only `GET /api/groups/{groupId}/audit-log?offset=0&take=30`, DTO page/entry и `AuditLogService`; запрос требует `ViewGroup`, возвращает события от новых к старым, `take` ограничен 100. Во frontend рабочем diff обновлены `openapi/backend.json` и `src/shared/api/generated/`, добавлены `entities/audit-log`, route `/groups/:groupId/history`, mobile page и переход из группы. Страница поддерживает загрузку следующих страниц. Эти изменения не commit/push до ручной проверки пользователя.

## Цель продукта

Telegram Mini App для совместных расходов: группы, траты с плательщиком и долями участников, прямые платежи, история, текущие балансы и минимизированный набор итоговых переводов.

## Доменная семантика

- `Group` — агрегат совместных расходов, опционально связан с Telegram-чатом.
- `Expense.CreatedBy` — плательщик траты.
- `ExpenseShare` — доля участника. Сумма всех долей должна совпадать с суммой траты; доли не-плательщиков создают долг плательщику.
- `Payment` — фактический перевод `FromUser -> ToUser`, прямой либо связанный с тратой.
- `GroupInvite` — токен для вступления в группу, хранимый в БД только в виде хеша и действующий до истечения срока.
- MVP работает только с рублями: без поля currency, конвертаций и нескольких валют внутри группы.
- `ExpenseShare.IsPaid` не хранится в БД: поле ответа вычисляется из платежей этой доли; payer share считается закрытой автоматически.
- У `ExpenseShare` есть отдельный `IsManuallySettled`: это ручной визуальный статус доли, не денежная операция. Он доступен только пока доля не закрыта платежами, не меняет balances/transfers и может быть возвращён в состояние долга. В ответе API различаются `IsPaidByPayments`, `IsManuallySettled` и итоговый `IsPaid`; все изменения попадают в audit history.
- Положительный баланс означает «пользователю должны», отрицательный — «пользователь должен».
- Итоговые transfers рассчитываются по чистым балансам и устраняют встречные/циклические переводы.

## Аутентификация Mini App

- Все controller endpoints под `/api` требуют Telegram authentication.
- Production принимает `X-Telegram-Init-Data` и проверяет Telegram HMAC с `TelegramAuth:BotToken`; `auth_date` по умолчанию действует 24 часа.
- `Development` принимает `X-Telegram-Dev-User-Id` только для локальной разработки. Это Telegram ID, не внутренний UUID пользователя.
- Подтверждённый Telegram ID хранится в `HttpContext.User` как `ClaimTypes.NameIdentifier` и `telegram_id` claim.
- Group authorization реализуется через membership и точечные permissions, хранимые в `GroupMemberPermissions`.
- `RequireGroupPermission` на action требует все перечисленные права (AND) и намеренно не допускает повторения на одном action. Если когда-нибудь понадобится статическое OR, будет введён отдельный явно названный `RequireAnyGroupPermission`; сейчас такого кейса нет. Проверки `own/any` для уже существующей траты или платежа остаются в `IGroupAuthorizationService`, так как требуют загрузить автора записи.
- В ответе group details `Members` содержит пользователя, его permissions, вычисленную UI-роль и флаг owner. Роли — только presets для UI; источником истины остаются permissions.
- Users API предназначен только для чтения собственного профиля: `GET /api/users/me`. Пользователь создаётся при первой успешной Telegram authentication; `DisplayName` и `Username` являются данными Telegram и не редактируются через API. Публичного поиска, списка и ручного создания пользователей нет.
- `User.DisplayName` и `User.Username` берутся из подписанного Telegram `initData`, сохраняются в БД и обновляются только при изменении профиля. Для этого не выполняется запрос к Telegram на каждый API-вызов; имя бота для invite-ссылок при необходимости один раз получает `getMe` и кэширует в памяти.

## Принятая структура репозиториев

Backend, Mini App и Telegram adapter развиваются в отдельных репозиториях. Это осознанный выбор ради чистой истории и независимого представления frontend/backend на GitHub:

```text
/Users/max/RiderProjects/
  BudgetSplitterWebApi/        # github.com/msLoginoffTeam/TelegramSplitter
  TelegramSplitterMiniApp/     # отдельный React + TypeScript repository
  tg_splitter_adapter/         # github.com/msLoginoffTeam/tg_splitter_adapter
```

Frontend не пишет API-контракты вручную: OpenAPI snapshot и TypeScript/React Query client генерируются скриптом. Backend `docker-compose.yml` поднимает только db+api; frontend имеет независимый Compose и настраивает адрес API через `API_UPSTREAM`. Подробности: `docs/LOCAL_DEVELOPMENT.md`.

Для production есть отдельный `compose.production.yml`: API и frontend соединяются внешней Docker-сетью `splitter-internal`, PostgreSQL не публикуется, frontend получает публичный HTTPS только через Tailscale Funnel. Push в `main` после тестов разворачивается self-hosted GitHub Actions runner с label `splitter-prod`. Полная инструкция: `docs/DEPLOYMENT_WINDOWS.md`.

## Направление реализации

1. Изменения backend и frontend сейчас ведутся прямо в `main`.
2. Backend P0 завершён: group authorization, денежные инварианты, транзакции и constraints в БД покрыты тестами.
3. Обновить OpenAPI-контракт после денежного блока и включить его в frontend handoff.
4. Реализовать отдельный React + TypeScript Mini App: сначала каркас ключевых сценариев — список и создание групп, затем dashboard, траты, платежи, transfers, members/settings.
5. Сократить Go-бот до Telegram entrypoint: `/start`, запуск Mini App, group deep links, приглашения, уведомления и публикация итогов.
6. Проверить независимые Compose-сценарии backend и frontend, затем E2E и только после этого VPS deployment.

## Запуск Mini App из Telegram

- Основные входы: profile/main Mini App, menu button и `startapp` deep links.
- Для групп MVP: бот публикует кнопку/deep link с подписанным group/invite token; backend валидирует пользователя и доступ к группе.
- Bot API не предоставляет полный список исторических участников чата. Автосинхронизация чата поэтому должна опираться на доступных администраторов и события новых участников/активности либо на отдельную MTProto-интеграцию.
- Параметры Telegram-клиента и `chat_instance` не являются авторизацией. Сервер валидирует `Telegram.WebApp.initData`.
- Attachment Menu не является обязательной частью MVP, так как production-доступ ограничен Telegram.
- Документация: <https://core.telegram.org/bots/webapps>.

## Критические инварианты

- Сумма долей равна `TotalAmount`, один пользователь встречается в долях не более одного раза (защищено unique index в БД).
- Плательщик и участники состоят в группе; суммы положительны, а отправитель и получатель payment различаются.
- Сумма балансов группы равна нулю.
- Предложенные transfers полностью обнуляют балансы.
- Изменение/удаление payment не оставляет ложный `IsPaid`, потому что `IsPaid` вычисляемое.
- Каждый endpoint проверяет Telegram-пользователя и доступ к группе.
- Повтор create-запроса не создаёт дубликат операции.

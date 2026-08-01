# Telegram Splitter — контекст проекта

Обновлено: 2026-08-02.

## Карта знаний

- Этот файл: архитектура, доменная модель, состояние репозиториев и план.
- `docs/KNOWN_ISSUES.md`: подтверждённые баги, риски и открытые продуктовые решения.
- `docs/TESTING.md`: запуск тестов и reusable integration-test infrastructure.
- Корневой `AGENTS.md`: обязательный указатель для следующих сессий Codex.

## Канонические репозитории

- Backend: <https://github.com/msLoginoffTeam/TelegramSplitter>
  - активная локальная копия: `/Users/max/RiderProjects/BudgetSplitterWebApi`;
  - `main`: `becaea4` (`0.0.47`), hash совпадает с GitHub на 2026-08-01;
  - remote настроен через SSH, но SSH-ключ недоступен из текущей Codex-среды; HTTPS-проверка работает.
- Telegram adapter: <https://github.com/msLoginoffTeam/tg_splitter_adapter>
  - `main`: `dd8e487` на момент проверки;
  - Go 1.23.5, long polling, сгенерированный OpenAPI-клиент.
- Frontend/Mini App: <https://github.com/msLoginoffTeam/TelegramSplitterMiniApp>
  - локальная копия: `/Users/max/RiderProjects/TelegramSplitterMiniApp`;
  - public repository создан 2026-08-02, baseline context commit: `d89e6a2`.
- Устаревший локальный дубль `/Users/max/RiderProjects/TelegramSplitter` удалён пользователем.

## Техническое состояние

- ASP.NET Core / EF Core / PostgreSQL 16, target `net9.0`.
- Backend собирается успешно, но остаются четыре compiler warning: nullable navigation `Group.CreatedBy`, два `async` без `await` и неверный XML `param` в `ExpensesController`.
- Есть 4 unit и 4 integration tests. Integration suite использует xUnit, `WebApplicationFactory`, Testcontainers PostgreSQL и Respawn; CI запускает их на push/PR.
- Go-бот собирается (`go test ./...`), но содержательных тестов нет.
- Docker CLI и OrbStack доступны; Compose разделён между репозиториями.
- В активной копии есть пользовательское незакоммиченное изменение `appsettings.Development.json`; не перезаписывать и не выводить его секреты.

## Цель продукта

Telegram Mini App для совместных расходов: группы, траты с плательщиком и долями участников, прямые платежи, история, текущие балансы и минимизированный набор итоговых переводов.

## Доменная семантика

- `Group` — агрегат совместных расходов, опционально связан с Telegram-чатом.
- `Expense.CreatedBy` — плательщик траты.
- `ExpenseShare` — доля участника. Сумма всех долей должна совпадать с суммой траты; доли не-плательщиков создают долг плательщику.
- `Payment` — фактический перевод `FromUser -> ToUser`, прямой либо связанный с тратой.
- Положительный баланс означает «пользователю должны», отрицательный — «пользователь должен».
- Итоговые transfers рассчитываются по чистым балансам и устраняют встречные/циклические переводы.

## Аутентификация Mini App

- Все controller endpoints под `/api` требуют Telegram authentication.
- Production принимает `X-Telegram-Init-Data` и проверяет Telegram HMAC с `TelegramAuth:BotToken`; `auth_date` по умолчанию действует 24 часа.
- `Development` принимает `X-Telegram-Dev-User-Id` только для локальной разработки. Это Telegram ID, не внутренний UUID пользователя.
- Подтверждённый Telegram ID хранится в `HttpContext.User` как `ClaimTypes.NameIdentifier` и `telegram_id` claim.
- Authentication не заменяет group authorization: доступ к каждой конкретной группе будет добавлен следующим этапом.

## Принятая структура репозиториев

Backend, Mini App и Telegram adapter развиваются в отдельных репозиториях. Это осознанный выбор ради чистой истории и независимого представления frontend/backend на GitHub:

```text
/Users/max/RiderProjects/
  BudgetSplitterWebApi/        # github.com/msLoginoffTeam/TelegramSplitter
  TelegramSplitterMiniApp/     # отдельный React + TypeScript repository
  tg_splitter_adapter/         # github.com/msLoginoffTeam/tg_splitter_adapter
```

Frontend не пишет API-контракты вручную: OpenAPI snapshot и TypeScript/React Query client генерируются скриптом. Backend `docker-compose.yml` поднимает только db+api; frontend имеет независимый Compose и настраивает адрес API через `API_UPSTREAM`. Подробности: `docs/LOCAL_DEVELOPMENT.md`.

## Направление реализации

1. Изменения временно ведутся прямо в `main` обоих репозиториев по решению владельца.
2. Стабилизировать backend: group authorization, денежные инварианты, транзакции, migrations и расширение tests.
3. Нормализовать OpenAPI и генерировать Go/TypeScript clients с CI drift check.
4. Реализовать отдельный React + TypeScript Mini App: список групп, dashboard, expense wizard, payments, transfers, members/settings.
5. Сократить Go-бот до Telegram entrypoint: `/start`, запуск Mini App, group deep links, приглашения, уведомления и публикация итогов.
6. Проверить независимые Compose-сценарии backend и frontend, затем E2E и только после этого VPS deployment.

## Запуск Mini App из Telegram

- Основные входы: profile/main Mini App, menu button и `startapp` deep links.
- Для групп MVP: бот публикует кнопку/deep link с подписанным group/invite token; backend валидирует пользователя и доступ к группе.
- Параметры Telegram-клиента и `chat_instance` не являются авторизацией. Сервер валидирует `Telegram.WebApp.initData`.
- Attachment Menu не является обязательной частью MVP, так как production-доступ ограничен Telegram.
- Документация: <https://core.telegram.org/bots/webapps>.

## Критические инварианты

- Сумма долей равна `TotalAmount`, один пользователь встречается в долях не более одного раза.
- Плательщик и участники состоят в группе; суммы строго положительны; `FromUser != ToUser`.
- Сумма балансов группы равна нулю.
- Предложенные transfers полностью обнуляют балансы.
- Изменение/удаление payment не оставляет ложный `IsPaid`.
- Каждый endpoint проверяет Telegram-пользователя и доступ к группе.
- Повтор create-запроса не создаёт дубликат операции.

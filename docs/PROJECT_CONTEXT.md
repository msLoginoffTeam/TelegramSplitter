# Telegram Splitter — контекст проекта

Обновлено: 2026-08-01.

## Карта знаний

- Этот файл: архитектура, доменная модель, состояние репозиториев и план.
- `docs/KNOWN_ISSUES.md`: подтверждённые баги, риски и открытые продуктовые решения.
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
- Backend собирается успешно; остаются два compiler warning: nullable `Group.CreatedBy` и неверный XML `param` в `ExpensesController`.
- Go-бот собирается (`go test ./...`), но содержательных тестов нет.
- Docker CLI установлен; Docker daemon во время проверки не был запущен.
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

## Принятая структура репозиториев

Backend, Mini App и Telegram adapter развиваются в отдельных репозиториях. Это осознанный выбор ради чистой истории и независимого представления frontend/backend на GitHub:

```text
/Users/max/RiderProjects/
  BudgetSplitterWebApi/        # github.com/msLoginoffTeam/TelegramSplitter
  TelegramSplitterMiniApp/     # отдельный React + TypeScript repository
  tg_splitter_adapter/         # github.com/msLoginoffTeam/tg_splitter_adapter
```

Frontend не пишет API-контракты вручную: OpenAPI snapshot и TypeScript/React Query client генерируются скриптом. Сгенерированный код или snapshot фиксируется в frontend-репозитории, а CI проверяет отсутствие drift. Backend Compose остаётся ответственным за db+api; frontend имеет свой Dockerfile/dev server. При необходимости единый локальный `compose.full.yaml` в backend может ссылаться на соседнюю frontend-папку относительным путём без создания третьего orchestration-репозитория.

## Направление реализации

1. Создать рабочие feature-ветки в backend и уже созданном frontend-репозитории.
2. Стабилизировать backend: Telegram `initData` auth, group authorization, денежные инварианты, транзакции, migrations и тесты.
3. Нормализовать OpenAPI и генерировать Go/TypeScript clients с CI drift check.
4. Реализовать отдельный React + TypeScript Mini App: список групп, dashboard, expense wizard, payments, transfers, members/settings.
5. Сократить Go-бот до Telegram entrypoint: `/start`, запуск Mini App, group deep links, приглашения, уведомления и публикация итогов.
6. Поднять единый локальный Compose, затем E2E и только после этого VPS deployment.

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

# Project guidance for Codex

This directory is the canonical active checkout of the Telegram Splitter backend.

Before planning or changing code, read:

1. `docs/PROJECT_CONTEXT.md` — architecture, repository decisions, domain semantics, current state and next steps.
2. `docs/KNOWN_ISSUES.md` — confirmed bugs, risks and unresolved product decisions.

Keep both documents current after material discoveries or architectural decisions. Add newly confirmed defects to `docs/KNOWN_ISSUES.md`; do not bury them only in chat history.

The outdated duplicate checkout at `/Users/max/RiderProjects/TelegramSplitter` was removed. The Mini App intentionally lives in the separate sibling repository `/Users/max/RiderProjects/TelegramSplitterMiniApp`; do not move frontend code into this backend repository unless the user changes that decision.

Preserve user-owned local configuration changes, especially `.env` and `appsettings.Development.json`. Never print or commit their secrets.

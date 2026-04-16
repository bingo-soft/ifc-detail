# Changelog

## [0.0.6] - 2026-04-16 12:01

### Summary
- Добавлен роутинг движков выполнения с политикой fast→baseline fallback без изменения JSON-контракта результата.

### Added
- Параметр CLI `--engine baseline|fast` с default-политикой выбора движка.
- Контракты и реализация `IProcessingEngine`, `EngineRouter`, `ExecutionDetails` и счётчиков попыток/успехов/fallback.
- Тесты `EngineRouterTests` для сценариев baseline, fast и fallback-классификации.

### Changed
- `Program` переведён на маршрутизацию через `EngineRouter` и вывод execution details (requested/effective engine, fallback reason, counters).

### Fixed
- Запись итогового JSON переведена на `File.Create(...)`, чтобы исключить хвост данных при перезаписи файла после fallback.

## 2026-04-16 — 0.0.5

### Added
- End-to-end parity tests for local real IFC files with automatic scenario selection (small/medium/large + edge candidates).
- Local IFC source configuration via `IFC_DETAIL_IFC_DIR` and input size limit via `IFC_DETAIL_MAX_IFC_MB`.

### Changed
- Extended `EndToEndParityTests` diagnostics: test output now includes selected files, filtered candidates, compatibility precheck results, and skip reasons.

### Fixed
- Selection pipeline now skips incompatible IFC files during precheck instead of failing whole parity suite.

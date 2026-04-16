# Changelog

## [0.0.7] - 2026-04-16 12:45

### Summary
- Реализован fast parsing слой без полной xbim object model с data-oriented индексами и декодированием IFC Unicode escape-последовательностей.

### Added
- Новый lightweight STEP parser `FastIfcStepParser` с выделенным scanner/builder pipeline.
- Data-oriented модель `StepEntityTable` (SoA массивы атрибутов и значений) и граф связей `EntityAdjacencyIndex` в формате offsets+edges.
- Новый fast JSON composer `FastIfcJsonComposer`, формирующий контрактные секции `materials/types/properties` напрямую из fast-модели.
- Тесты `FastParserPerformanceTests`:
  - parity на минимальном IFC;
  - проверка снижения alloc/времени на типовом датасете;
  - проверка декодирования IFC Unicode (`\X2\...\X0\`, `\X4\...\X0\`).

### Changed
- `FastMaterialExtractor` переведён с xbim `IfcStore.Open(...)` на fast-path: parse + compose.
- В fast-модели добавлен предрасчёт индекса layer->layerSet для исключения runtime-дублей relation-структур.

### Fixed
- Валидация schema regex в `EngineRouting` ограничена таймаутом `TimeSpan.FromSeconds(1)` для защиты от зависания regex.
- Строковые STEP-литералы в fast parser теперь декодируют IFC Unicode escape-последовательности в нормальный текст.

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

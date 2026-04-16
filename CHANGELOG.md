# Changelog

## [0.0.9] - 2026-04-16 15:47

### Added
- Добавлены CLI-параметры `--verbosity none|timing|detailed`, `--progress completed|remaining|none`, `--output-buffer-kb`, `--write-through[=true|false]`.
- Добавлены тесты `CliOptionsTests` для новых режимов CLI и валидации комбинаций флагов.
- Добавлен тип `OutputWriteOptions` и прокидывание параметров записи в baseline и fast pipeline.

### Changed
- Обновлён `--help`: описаны новые параметры, правила валидации и сохранение positional-вызова `<source.ifc> [target.json]`.
- Реализован вывод `verbosity=timing` с форматированием длительности в `ms/s/m:ss/h:mm:ss`.
- Реализован `verbosity=detailed`: вывод schema, requested/effective parser, fallback reason/count, counters, elapsed и peak memory.
- Запись JSON переведена на `FileStreamOptions` с настройкой буфера и режима write-through во всех движках.

### Fixed
- Для `--verbosity none` отключён runtime-вывод, включая прогресс и сообщения ошибок выполнения.
- Добавлена валидация несовместимой комбинации `--verbosity none` c `--progress completed|remaining`.

## [0.0.8] - 2026-04-16 13:52

### Added
- Добавлены тесты `DirectJsonEmitterTests` для двух режимов эмиссии: preserve-order и deterministic.
- Добавлена конфигурация fast extractor для выбора режима эмиссии и включения/отключения строгого dedup `last-occurrence-wins`.

### Changed
- Fast JSON emitter переведён на прямую эмиссию по индексам через промежуточные `EmissionItem` без сборки JSON-документа в память.
- Добавлены режимы вывода: `JsonEmissionMode.PreserveOrder` и `JsonEmissionMode.Deterministic`.
- Для deterministic режима реализована стабильная сортировка ключей по `StringComparer.Ordinal`.

### Fixed
- Устранена запись дублей на уровне секций `materials/types/properties` при включённом строгом dedup, соблюдён контракт `last-occurrence-wins`.
- Сохранён legacy-путь fast extractor без dedup для совместимости с текущим baseline parity потоком.

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

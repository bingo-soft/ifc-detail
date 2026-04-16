# Changelog

## [Unreleased]

### Added

### Changed

### Fixed

### Removed

### Security

## [0.0.13] - 2026-04-16 18:19

### Added
- Added a repository policy rule requiring English for all documents and text files.

### Changed
- Translated project documentation and policy files to English (`README.md`, `optimization-proposals.md`, `changelog_policy.md`).
- Translated existing changelog entries to English for consistency.

### Fixed
- Updated Unicode escape coverage in `FastParserPerformanceTests` to validate English decoded text (`By default`) instead of Russian text.

## [0.0.12] - 2026-04-16 18:05

### Added
- Added `README.md` with up-to-date CLI parameters, run profiles, and the recommended `fast + none` setup for large IFC files.

### Changed
- Switched the fast parser to streaming mode for `--intermediate-store none` without `File.ReadAllText`, with selective parsing of relevant IFC types only.
- Changed the default `--intermediate-store` for fast/default policy to `none`.
- Updated CLI help and option-parsing tests for the new intermediate-store default.

### Fixed
- Reduced peak allocations on the fast path by removing redundant string caches and empty value lists in the intermediate STEP model.

## [0.0.11] - 2026-04-16

### Added
- Added memory-scaling options for the fast parser: `MemoryScalingOptions`, `none|mmf` modes, read segmentation (`--segment-size-kb`), and spill directory (`--spill-dir`).
- Added an MMF parity test for the fast path on a synthetic typical IFC dataset.

### Changed
- Changed the default `--intermediate-store` for fast/default policy to `mmf`; baseline default remains `none`.
- Updated CLI help with intermediate-store options and clarified validation rules.
- Updated engine routing and fast extractor to pass `MemoryScalingOptions` through the full pipeline.

### Fixed
- Fixed MMF spill path: the writer is now closed before reading spill files, removing `IOException` and baseline fallback on large IFC files.
- Accelerated DATA/ENDSEC streaming in MMF mode by removing redundant string allocations in the character loop and stabilizing STEP entity detection in the stream parser.

## [0.0.10] - 2026-04-16 16:01

### Added
- Added string-normalization caching in fast STEP builder (`_typeNameCache`, `_stringNormalizationCache`) for repeated values.
- Added `GlobalId` caching in `FastIfcDataModel` for repeated access in the JSON emitter.
- Added `ArrayPool<char>` usage in STEP string-literal parsing for temporary buffers.

### Changed
- Cleaned baseline extractor hot path from LINQ `Concat` chains by switching type enumeration to sequential `foreach` via `EmitEntities`.
- Switched numeric/token parsing in `FastIfcStepParser.Scanner` to `Span/ReadOnlySpan` (`ParseNumber`, `ParseIdentifier`, `ParseRawToken`) to reduce intermediate strings.
- Switched IFC Unicode escape decoding to `ReadOnlySpan<char>` with character-wise hex parsing and no `Substring`/`char.ConvertFromUtf32`.

### Fixed
- `ParseInt` now validates that digits are present and throws `FastParseHeaderException` instead of implicitly returning an invalid value.
- Added Unicode code point range validation when decoding escape blocks.

## [0.0.9] - 2026-04-16 15:47

### Added
- Added CLI parameters `--verbosity none|timing|detailed`, `--progress completed|remaining|none`, `--output-buffer-kb`, `--write-through[=true|false]`.
- Added `CliOptionsTests` for new CLI modes and flag-combination validation.
- Added `OutputWriteOptions` and propagated output write settings through baseline and fast pipelines.

### Changed
- Updated `--help` with new options, validation rules, and positional invocation `<source.ifc> [target.json]`.
- Implemented `verbosity=timing` output with duration formatting in `ms/s/m:ss/h:mm:ss`.
- Implemented `verbosity=detailed` output: schema, requested/effective parser, fallback reason/count, counters, elapsed, and peak memory.
- Switched JSON writing to `FileStreamOptions` with configurable buffering and write-through mode in all engines.

### Fixed
- Disabled runtime output for `--verbosity none`, including progress and runtime error messages.
- Added validation for incompatible combination `--verbosity none` with `--progress completed|remaining`.

## [0.0.8] - 2026-04-16 13:52

### Added
- Added `DirectJsonEmitterTests` for two emission modes: preserve-order and deterministic.
- Added fast extractor configuration to choose emission mode and enable/disable strict `last-occurrence-wins` dedup.

### Changed
- Switched fast JSON emitter to direct index-based emission via intermediate `EmissionItem` objects without building a full JSON document in memory.
- Added output modes: `JsonEmissionMode.PreserveOrder` and `JsonEmissionMode.Deterministic`.
- Implemented stable key sorting by `StringComparer.Ordinal` for deterministic mode.

### Fixed
- Removed duplicate writes in `materials/types/properties` sections when strict dedup is enabled, while preserving `last-occurrence-wins` behavior.
- Kept legacy fast extractor path without dedup for compatibility with current baseline parity flow.

## [0.0.7] - 2026-04-16 12:45

### Summary
- Implemented a fast parsing layer without full xbim object model, using data-oriented indexes and IFC Unicode escape decoding.

### Added
- New lightweight STEP parser `FastIfcStepParser` with dedicated scanner/builder pipeline.
- Data-oriented model `StepEntityTable` (SoA arrays of attributes and values) and relation graph `EntityAdjacencyIndex` in offsets+edges format.
- New fast JSON composer `FastIfcJsonComposer` that builds contract sections `materials/types/properties` directly from the fast model.
- `FastParserPerformanceTests`:
  - parity on minimal IFC;
  - allocation/time reduction check on a typical dataset;
  - IFC Unicode decoding verification (`\X2\...\X0\`, `\X4\...\X0\`).

### Changed
- Moved `FastMaterialExtractor` from xbim `IfcStore.Open(...)` to fast path: parse + compose.
- Added precomputed layer->layerSet index in the fast model to avoid runtime duplicates in relation structures.

### Fixed
- Limited schema regex validation in `EngineRouting` with `TimeSpan.FromSeconds(1)` timeout to prevent regex hangs.
- STEP string literals in the fast parser now decode IFC Unicode escape sequences into normal text.

## [0.0.6] - 2026-04-16 12:01

### Summary
- Added execution engine routing with fast→baseline fallback policy, without changing the output JSON contract.

### Added
- CLI option `--engine baseline|fast` with default engine-selection policy.
- Contracts and implementation for `IProcessingEngine`, `EngineRouter`, `ExecutionDetails`, and attempt/success/fallback counters.
- `EngineRouterTests` for baseline, fast, and fallback-classification scenarios.

### Changed
- Switched `Program` to route through `EngineRouter` and print execution details (requested/effective engine, fallback reason, counters).

### Fixed
- Switched final JSON write to `File.Create(...)` to prevent stale trailing data when overwriting output after fallback.

## 2026-04-16 — 0.0.5

### Added
- End-to-end parity tests for local real IFC files with automatic scenario selection (small/medium/large + edge candidates).
- Local IFC source configuration via `IFC_DETAIL_IFC_DIR` and input size limit via `IFC_DETAIL_MAX_IFC_MB`.

### Changed
- Extended `EndToEndParityTests` diagnostics: test output now includes selected files, filtered candidates, compatibility precheck results, and skip reasons.

### Fixed
- Selection pipeline now skips incompatible IFC files during precheck instead of failing whole parity suite.


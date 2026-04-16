# Changelog

## 2026-04-16 — 0.0.5

### Added
- End-to-end parity tests for local real IFC files with automatic scenario selection (small/medium/large + edge candidates).
- Local IFC source configuration via `IFC_DETAIL_IFC_DIR` and input size limit via `IFC_DETAIL_MAX_IFC_MB`.

### Changed
- Extended `EndToEndParityTests` diagnostics: test output now includes selected files, filtered candidates, compatibility precheck results, and skip reasons.

### Fixed
- Selection pipeline now skips incompatible IFC files during precheck instead of failing whole parity suite.

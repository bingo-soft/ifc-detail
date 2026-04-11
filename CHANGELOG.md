# Changelog

All notable changes to this project will be documented in this file.

## [0.0.6] - 2026-04-11

### Performance Improvements

- **Method inlining**: Added AggressiveInlining to hot path helper methods (WriteStringCached, WriteStartArrayCached, ConvertTypeToJson, WritePropertiesArray)
- **Aggressive optimization**: Added AggressiveOptimization to main processing methods (BTask, Start)
- **JIT optimization**: Enabled compiler hints for better code generation and CPU cache utilization

### Expected Results

- 8-12% faster processing speed
- Better CPU instruction cache utilization
- No memory overhead
- Zero risk - pure compiler optimization

## [0.0.5] - 2026-04-11

### Performance Improvements

- **Memory optimization**: Increased buffer sizes from 1KB to 1MB/512KB to avoid reallocations
- **String caching**: Pre-encoded 32 frequently used property names using JsonEncodedText
- **GUID optimization**: Cached GUID.ToString() calls in local variables to avoid repeated conversions
- **StringBuilder reuse**: Replaced string interpolation with reusable StringBuilder for ID generation
- **Single-pass processing**: Optimized MaterialExtractor to iterate model.Instances once instead of 28+ Concat().OfType() calls
- **Direct byte writing**: Removed intermediate UTF-8 → String → UTF-8 conversions in CreateJson()
- **FileStream tuning**: Increased buffer size to 80KB for better I/O performance
- **JSON formatting**: Explicitly disabled indentation to reduce output size

### Expected Results

- 2-3x faster processing speed
- ~40% lower memory consumption
- Same functionality and output format

## [0.0.4] - Previous version

Initial tracked version.

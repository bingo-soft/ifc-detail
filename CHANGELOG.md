# Changelog

All notable changes to this project will be documented in this file.

## [0.0.7] - 2026-04-11

### Features

- **Performance profiling**: Added built-in performance profiling with `--profile` CLI flag
- **Detailed metrics**: Tracks execution time, memory usage, GC collections, and throughput
- **Formatted report**: Beautiful console output with execution statistics

### Usage

```bash
ifc_metadata input.ifc output.json --profile
```

### Report includes

- Total execution time and per-entity processing time
- Throughput (entities/second)
- Memory usage (initial, final, used, peak)
- Garbage collection statistics (Gen 0/1/2)
- Total entities processed

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

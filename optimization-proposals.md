# Extreme IFC→JSON Optimization Proposals

## 1) Extreme optimizations in the current approach (including “dirty” ones)

### A. Remove the main memory source: `string` per token
- Store `offset+length` in a shared byte/char buffer instead of `string`.
- Decode to `string` only when writing JSON.
- Do not materialize values that are outside the JSON contract.

Effect: significant heap reduction (often -30..-60% for text-heavy data).

### B. Two-pass mode for large files
**Pass 1 (indexing):**
- Keep only the minimum: `entityId -> fileOffset`, `entityId -> typeCode`, compact references for relevant types only.
- Do not store full argument payloads.

**Pass 2 (emission):**
- Re-read entities by offset.
- Write JSON in streaming mode.

Effect: major memory reduction at the cost of extra I/O.

### C. Represent types as `enum/int` instead of `string`
- Parse type names into `EntityTypeCode`.
- Use switch/filters by int.
- Keep one table `int -> utf8-name`.

Effect: fewer allocations, faster branching.

### D. Remove `double` where not required
- For JSON, store numbers as slices of original STEP text (offset/length).
- Normalize only where required.

Effect: lower CPU and allocations for numeric parsing.

### E. `Utf8JsonWriter` + `IBufferWriter<byte>` + preencoded names
- Use `JsonEncodedText` for recurring property names.
- Minimize intermediate `string` allocations.
- Write directly from `ReadOnlySpan<byte>` where possible.

Effect: moderate CPU and GC reduction.

### F. Pooling and rented memory everywhere
- `ArrayPool<T>` for argument lists, bracket stacks, temporary buffers.
- `ValueListBuilder<T>`-like structures for short lists.
- Strict return-to-pool via `try/finally`.

Risk: use-after-return/data corruption.

### G. Unsafe/Span scanner (dirty but fast)
- `unsafe` + pointers to mmap/view buffers.
- ASCII fast-path with minimal checks.
- Manual DFA parser without recursion.

Effect: maximum CPU performance, but fragile code.

### H. Strict selective extraction
- If only `materials/types/properties` is needed, ignore everything else at character-scan level.
- Do not build a generic STEP AST.

Effect: maximum speed/memory gains for a fixed contract.

### I. External sort/dedup without RAM growth
- Write `key+offset` pairs into a temp file.
- Run external sort (chunk + merge).
- Emit final JSON by sorted key.

Effect: low RAM for large files in deterministic mode.

### J. Runtime/GC tricks
- `GCSettings.LatencyMode = SustainedLowLatency` for hot stages.
- `GC.TryStartNoGCRegion(...)` for short windows.
- Server GC + affinity.

Risk: instability and tail-latency degradation.

---

## 2) “From-scratch” architecture for fast IFC→JSON with low memory

### Goal
Do not parse the whole IFC model. Extract only data required by the target contract.

### Layers

1. **Input Layer**
   - `MemoryMappedFile` or buffered stream.
   - One scanner over `ReadOnlySpan<byte>` (preferably UTF-8).

2. **Lexer/Scanner (DFA)**
   - Allocation-free tokens: `offset/len/type`.
   - Strings/numbers as slices, not `string/double`.

3. **Selective Entity Parser**
   - Parse `#id=TYPE(...)`.
   - Immediately discard irrelevant types.
   - For relevant ones, extract only required fields/references.

4. **Compact Index Store**
   - SoA: `ids[]`, `typeCodes[]`, `argRefOffsets[]`, `argRefValues[]`.
   - Text references as `(bufferId, offset, len)`.
   - Spill into temp binary pages at memory limit.

5. **Reference Resolver**
   - Minimal maps: `id -> compactIndex`.
   - Specialized indexes only for required relations (for example layer->layerSet).
   - No full entity graph.

6. **Emitter Pipeline**
   - Streaming JSON output by sections.
   - Separate key-index stage for deterministic/dedup mode.
   - No DOM and no `JsonDocument`.

7. **Execution Modes**
   - `ultra-fast`: preserve order, no sort.
   - `low-mem`: two-pass + external structures.
   - `compat`: stricter validation/compatibility.

### Core principles
- No object graph of entities.
- No early string materialization.
- Filter entity types as early as possible.
- Single-writer JSON.
- Data-oriented SoA.
- Explicit memory budget (for example 512MB) + spill.

### Implementation priority (highest impact)
1. `offset/length` instead of `string`.
2. Two-pass selective extraction.
3. `int` type codes + zero-allocation scanner.
4. External sort/dedup (if deterministic output is required).
5. `unsafe` fast-path.


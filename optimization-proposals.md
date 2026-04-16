# Предложения по предельным оптимизациям IFC→JSON

## 1) Предельные оптимизации в текущем подходе (включая «грязные»)

### A. Убрать главный источник памяти: `string` для каждого токена
- Хранить `offset+length` в общем byte/char буфере вместо `string`.
- Декодировать в `string` только при записи JSON.
- Для значений вне контракта JSON не делать материализацию вообще.

Эффект: сильное снижение heap (часто -30..-60% на текстовых данных).

### B. Двухпроходный режим для больших файлов
**Проход 1 (индексация):**
- Сохранить минимум: `entityId -> fileOffset`, `entityId -> typeCode`, компактные ссылки только для нужных типов.
- Не хранить аргументы целиком.

**Проход 2 (эмиссия):**
- Подчитывать сущности по offset.
- Писать JSON потоково.

Эффект: резкое снижение памяти, цена — дополнительный I/O.

### C. Типы как `enum/int`, а не `string`
- Парсинг type name в `EntityTypeCode`.
- Switch/фильтры по int.
- Единая таблица `int -> utf8-name`.

Эффект: меньше аллокаций, быстрее ветвления.

### D. Убрать `double` там, где не нужно
- Для JSON хранить число как slice исходного STEP (offset/length).
- Нормализовать только где требуется.

Эффект: меньше CPU и аллокаций на numeric parsing.

### E. `Utf8JsonWriter` + `IBufferWriter<byte>` + preencoded names
- Использовать `JsonEncodedText` для повторяющихся property names.
- Минимизировать промежуточные `string`.
- По возможности писать из `ReadOnlySpan<byte>`.

Эффект: умеренное снижение CPU и GC.

### F. Пулы и аренда памяти везде
- `ArrayPool<T>` для списков аргументов, стеков скобок, временных буферов.
- `ValueListBuilder<T>`-подобные структуры для коротких списков.
- Строгий возврат в пул через `try/finally`.

Риск: use-after-return/повреждение данных.

### G. Unsafe/Span-сканер (грязный, но быстрый)
- `unsafe` + указатели на mmap/view buffer.
- ASCII fast-path без тяжёлых проверок.
- Ручной DFA parser без рекурсии.

Эффект: максимум CPU, но высокая хрупкость кода.

### H. Жёсткий selective extraction
- Если нужен только контракт `materials/types/properties`, игнорировать остальное на символном уровне.
- Не строить общий STEP AST.

Эффект: максимум speed/memory при фиксированном контракте.

### I. Внешняя сортировка/дедуп без RAM
- Писать `key+offset` во временный файл.
- Делать external sort (chunk + merge).
- Эмитить финальный JSON по отсортированному ключу.

Эффект: низкая RAM на больших файлах при deterministic режиме.

### J. Runtime/GC трюки
- `GCSettings.LatencyMode = SustainedLowLatency` на hot stage.
- `GC.TryStartNoGCRegion(...)` для короткого окна.
- Server GC + affinity.

Риск: нестабильность и деградация в хвостах.

---

## 2) Архитектура «с нуля» для fast IFC→JSON с низкой памятью

### Цель
Не парсить весь IFC целиком, а извлекать только данные целевого контракта.

### Слои

1. **Input Layer**
   - `MemoryMappedFile` или buffered stream.
   - Единый scanner над `ReadOnlySpan<byte>` (предпочтительно UTF-8).

2. **Lexer/Scanner (DFA)**
   - Токены без аллокаций: `offset/len/type`.
   - Строки/числа — slices, не `string/double`.

3. **Selective Entity Parser**
   - Чтение `#id=TYPE(...)`.
   - Мгновенный discard нерелевантных типов.
   - Для релевантных извлекать только нужные поля/ссылки.

4. **Compact Index Store**
   - SoA: `ids[]`, `typeCodes[]`, `argRefOffsets[]`, `argRefValues[]`.
   - Текст: `(bufferId, offset, len)`.
   - Spill в temp binary pages при лимите памяти.

5. **Reference Resolver**
   - Минимальные карты: `id -> compactIndex`.
   - Спец-индексы только для нужных relation (например layer->layerSet).
   - Без общего графа.

6. **Emitter Pipeline**
   - Потоковая запись JSON по секциям.
   - Для deterministic/dedup — отдельный key-index stage.
   - Без DOM и без `JsonDocument`.

7. **Execution Modes**
   - `ultra-fast`: preserve order, без sort.
   - `low-mem`: two-pass + external structures.
   - `compat`: более строгая валидация/совместимость.

### Базовые принципы
- Без объектного графа сущностей.
- Без ранней материализации строк.
- Фильтрация типов как можно раньше.
- Single-writer JSON.
- Data-oriented SoA.
- Явный memory budget (например 512MB) + spill.

### Приоритет внедрения (максимальный эффект)
1. `offset/length` вместо `string`.
2. Two-pass selective extraction.
3. `int` type codes + zero-alloc scanner.
4. External sort/dedup (если нужен deterministic).
5. `unsafe` fast-path.

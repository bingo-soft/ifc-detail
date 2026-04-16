# ifc-detail

CLI-инструмент для извлечения данных из IFC в JSON (секции `materials`, `types`, `properties`).

## Требования

- .NET SDK 10.0

## Сборка

```powershell
dotnet build ifc-detail.sln -c Release
```

## Запуск

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>"
```

Если `target.json` не указан, создаётся файл рядом с IFC с тем же именем и расширением `.json`.

## Ключевые параметры CLI

```text
--engine baseline|fast
--verbosity none|timing|detailed
--progress completed|remaining|none
--output-buffer-kb <positive-int>
--write-through[=true|false]
--intermediate-store none|mmf
--segment-size-kb <positive-int>
--spill-dir <path>
```

## Текущее поведение по умолчанию

- Политика движка: `fast` с fallback на `baseline`.
- `--intermediate-store` по умолчанию: `none`.
- `--segment-size-kb` и `--spill-dir` допустимы только вместе с `--intermediate-store mmf`.

## Рекомендуемый профиль для больших IFC

Для максимальной скорости и умеренной памяти используйте:

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>" --engine fast --intermediate-store none --verbosity detailed
```

## Профили запуска

### 1) Baseline (контрольный)

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>" --engine baseline --verbosity detailed
```

### 2) Fast + none (предпочтительный)

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>" --engine fast --intermediate-store none --verbosity detailed
```

### 3) Fast + mmf (при необходимости spill/сегментации)

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>" --engine fast --intermediate-store mmf --segment-size-kb 1024 --spill-dir "<temp_dir>" --verbosity detailed
```

## Формат вывода

JSON-объект с секциями:

- `materials`
- `types`
- `properties`

## Тесты

```powershell
dotnet test tests/IfcDetail.Tests/IfcDetail.Tests.csproj
```

# ifc-detail

CLI tool for extracting data from IFC into JSON (sections: `materials`, `types`, `properties`).

## Requirements

- .NET SDK 10.0

## Build

```powershell
dotnet build ifc-detail.sln -c Release
```

## Run

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>"
```

If `target.json` is not specified, a file is created next to the IFC file with the same name and the `.json` extension.

## Key CLI parameters

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

## Current default behavior

- Engine policy: `fast` with fallback to `baseline`.
- Default `--intermediate-store`: `none`.
- `--segment-size-kb` and `--spill-dir` are valid only with `--intermediate-store mmf`.

## Recommended profile for large IFC files

For maximum speed with moderate memory usage:

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>" --engine fast --intermediate-store none --verbosity detailed
```

## Run profiles

### 1) Baseline (control)

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>" --engine baseline --verbosity detailed
```

### 2) Fast + none (preferred)

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>" --engine fast --intermediate-store none --verbosity detailed
```

### 3) Fast + mmf (when spill/segmentation is required)

```powershell
dotnet run --project src/ifc-detail.csproj -- "<source.ifc>" "<target.json>" --engine fast --intermediate-store mmf --segment-size-kb 1024 --spill-dir "<temp_dir>" --verbosity detailed
```

## Output format

JSON object with sections:

- `materials`
- `types`
- `properties`

## Tests

```powershell
dotnet test tests/IfcDetail.Tests/IfcDetail.Tests.csproj
```

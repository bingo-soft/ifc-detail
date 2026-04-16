## 🔧 Build handling

### 1. Pre-release builds

If running in CI (non-release):

```id="c1"
2.MINOR.PATCH-buildNumber
```

Example:

```id="c2"
2.7.0-123
```

Rules:

* buildNumber = CI run number or incremental counter
* do NOT treat as final version
* do NOT write to stable changelog

---

### 2. Build metadata

For traceability:

```id="c3"
2.MINOR.PATCH+build.xxx
```

Example:

```id="c4"
2.7.0+build.123
```

Rules:

* does NOT affect version precedence
* safe for production

---

### 3. Priority

When generating version:

1. Determine base version (MINOR/PATCH)
2. If CI build:
   → append `-build`
3. If release:
   → optionally append `+build.xxx`

---

### 4. Restrictions

Forbidden:

```id="c5"
2.7.0-123+build.123
2.7.123
```

---

### 5. Example flow

Commits:

```id="c6"
feat: new export
```

CI:

```id="c7"
2.8.0-45
```

Release:

```id="c8"
2.8.0
```

With metadata:

```id="c9"
2.8.0+build.45
```

# Changelog Policy

Based on Keep a Changelog.
Adapted for frequent releases and automation.

---

## 📌 General principles

1. Maintain changelog in a single file: `CHANGELOG.md`.
2. Add new entries at the **top**.
3. Use **date + time**.
4. Source of truth: **Conventional Commits**.
5. Changelog must be readable by humans (not only machines).
6. All repository documents and text files must be written in English.

---

## 📦 File structure

```id="cl1"
# Changelog

## [Unreleased]

### Added
### Changed
### Fixed
### Removed
### Security

## [2.7.3] - 2026-04-16 18:42
...
```

---

## 🔄 Unreleased section

### Purpose

* accumulates changes between releases
* is automatically updated from commits
* is cleared during release

---

### Rules

* always stays at the **top**
* always exists
* contains only **not-yet-released changes**

---

## 🚀 Release process

When creating a new version:

1. Move `Unreleased` content into the new version section.
2. Add timestamp:

```id="cl2"
YYYY-MM-DD HH:mm
```

3. Recreate an empty `Unreleased` section.

---

### Example

Before release:

```id="cl3"
## [Unreleased]

### Added
- New API
```

After release:

```id="cl4"
## [Unreleased]

## [2.8.0] - 2026-04-16 18:42

### Added
- New API
```

---

## 🧱 Categories

Use standard categories:

* Added — new functionality
* Changed — behavior changes / improvements
* Fixed — bug fixes
* Removed — removed functionality
* Security — security changes

---

## 🔁 Conventional Commit mapping

| Commit   | Changelog          |
| -------- | ------------------ |
| feat     | Added              |
| fix      | Fixed              |
| perf     | Changed            |
| refactor | Changed            |
| docs     | (usually skipped)  |
| chore    | (optional)         |

---

## ⚠️ Breaking Changes

If a commit contains:

```id="cl5"
BREAKING CHANGE
```

Then:

* add it to `Changed`
* mark it explicitly:

```id="cl6"
### Changed
- ⚠️ API contract changed (breaking change)
```

---

## ⏱ Date format

Recommended:

```id="cl7"
YYYY-MM-DD HH:mm
```

Example:

```id="cl8"
2026-04-16 18:42
```

---

## 🔼 Entry order

* add newer versions above older ones
* inside sections, any order is acceptable, chronological is preferred

---

## 🤖 Automation

Changelog should:

* be generated from commits
* be updated in CI
* not be edited manually (except final cleanup)

---

## 🧠 Quality rules

* short and clear wording
* no technical noise (task IDs only when needed)
* developer-oriented language

---

## 🚫 Forbidden

* editing old released versions
* mixing unreleased and released changes
* writing changelog entries without commits
* duplicate entries

---

## ✅ Full changelog example

```id="cl9"
# Changelog

## [Unreleased]

### Fixed
- Fixed a rare race condition

---

## [2.7.3] - 2026-04-16 18:42

### Fixed
- Fixed crash at startup

---

## [2.7.2] - 2026-04-16 14:10

### Changed
- Query optimization
```


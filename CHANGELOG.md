# Changelog

All notable changes to **Pushback Creator App** are listed here. Versions follow [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`). Android `versionCode` is noted in parentheses.

## [1.2.0] — 2026-09-12

Android versionCode **102**.

### Added

- **Export KML** on the airport list: `{package}.kml` with a folder per airport, a helipad placemark, and a placemark per pushback slot (Google Earth `lon,lat,0`).
- User **GUIDE.md**, **CHANGELOG.md**, and **MIT** license for the public GitHub release.

### Notes

- A KML export does not clear the “unexported project” warning. Export TME or ZIP still does.

## [1.1.0] — 2026-09-12

Android versionCode **101**.

### Added

- Optional **`[sname]`** on dummy pushback airports (empty uses the TME code). Import assigns `[sname]` for pushback and heliport when it is not just the ICAO / TME code.
- **Add TME (FSG)** and **Add ZIP (FS4)** on the airport list to merge airports into the current project (duplicate TME codes skipped).

### Changed

- TSC/WAD **`[icao]`** for dummy pushback airports is the 4-character code plus two spaces (folder and file names stay `{icao}00`).
- Airport list **Cancel** renamed to **Quit project**.
- TSC parking section closer uses the same space indent as WAD. Export checks that every section opened with `<` (no `>` on that line) is closed with `>`.

## [1.0.0] — 2026-09-11

Android versionCode **100**. First tester build (TEST APK).

### Added

- New project, open FSG TME, open FS4 ZIP.
- Dummy pushback airports (`ICAO` + `00`) and 4-character heliports.
- Helipad position/heading; parking list (pushback only); helipad radius always **20** in TSC/WAD.
- Export Unix Store **TME** and Windows **ZIP (FS4)**.
- Google Earth decimal and DMS paste; WAD CalcES RAD conversion.
- Dark compact MAUI UI (Windows + Android). Quit via Shell flyout **Exit**.

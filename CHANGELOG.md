# Changelog

All notable changes to **Pushback Creator App** are listed here. Versions follow [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`). Android `versionCode` is noted in parentheses.

## [2.0.0] — unreleased (branch `2.0-devl`)

Android versionCode **200**.

### Added

- Airport editor **map** (Leaflet in a HybridWebView): Esri World Imagery XYZ tiles, optional OSM streets, no map API key.
- TGI-style split layout: inspector left / map right (wide), inspector top / map bottom (narrow).
- Place and move **helipad**, **tower**, **windsock**, and **pushback** objects on the map. Parking uses a heading-aligned rectangle (`size`); helipad uses a circle from **radius** (default 20 m). A **heading arrow** is drawn on placed objects that have a heading.
- **OpenStreetMap Nominatim** search only until the helipad (airport location) is set; the panel then hides. Tower, parking, windsock (and later XREF) stay unavailable until that location exists.
- Drag a footprint or the round handle to **move**. Drag the diamond on the heading arrow to set **heading** (helipad, parking, and windsock). The tower is a point with a pixel grab halo (no heading square). Move/rotate hit targets and the heading arrow are sized for finger use on a tablet.
- Optional **tower** on heliports, same TSC/WAD/`container` dummy as pushback airports.
- Helipad **radius** field (before heading); TSC/WAD `[radius]` is imported and exported; the map circle uses that value.
- Windsock **direction** and **scale factor** (before height offset) for future XREF objects. Map square orientation follows direction; scale sizes the placeholder square. `airportmast03` stays on helipad heading and fixed scale 2.5.
- Parking slots are edited in the inspector (no extra parking page in the map workflow).
- **XREF objects** export as a scenery **POI** (`scenery/poi/{e#####n####}_pca_{code}/`) with a buried `pca_anchor.tmb` / `pca_anchor.ttx` (ETC2 in TME for FSG OpenGL/Vulkan, DXT1 in ZIP for FS4) so autoheight uses terrain: heliport windsock, dummy helipad, and tower dummy; pushback **tower dummy** the same way. Airport TSC no longer links a `.toc`. Open still reads a legacy airport `.toc`.

### Changed

- Coordinate **input is decimal only** (`lat, lon` or `lat lon`). Degrees/minutes paste is rejected so Google Earth DMS cannot shift positions.
- Home screen Google Earth note: **Export KML**, plus optional decimal copy/paste after Google Earth **Settings → decimal**.
- Heliport name caption is **Optional** (short enough for phone portrait).
- Project screen export button is **Export TME (FSG)**, matching **Export ZIP (FS4)**.
- Map zoom goes to level **21** (imagery tiles stay native to 18; extra levels enlarge for a small phone). On a tablet, pinch and **+ / −** are enough without extra zoom. **Windows** uses the mouse wheel to zoom. Android emulator mouse wheel is unreliable; use pinch or **+ / −** on a device.
- Windows **Export** picks a **folder** (same idea as Mac) and writes `{package}.tme` / `.zip` / `.kml` there. Save no longer navigates into a highlighted subfolder; an existing file in that folder asks before replace.

## [1.3.0] — 2026-11-15

Android versionCode **103**.

### Added

- **Sort A–Z** on the airport list (next to **Quit project**): order by ICAO / TME code. Open/Add TME or ZIP no longer reorders airports.
- **Sort A–Z** on the airport parking list (by slot name). **Cancel** on the airport screen restores the previous parking order.
- **Mac / iOS export:** folder picker (Documents, Downloads, …) with a security-scoped bookmark, same approach as TgiModelTransformer.
- Optional **tower position shift** and **tower height AGL** on pushback airports. Height **0** writes no `.toc`; any other height writes `{icao}00.toc` (`container` at scale `0.0001`) and links it from the `.tsc` (`auto_height` / `use_height_offset`). **`[tower_position]`** is written in TSC and WAD (world units). **Export KML** adds a **Tower** pin. Tower position must not match the helipad or a parking slot.
- Optional **windsock** (`airport_windsock_large1`) and **helipad** (`airportmast03`, scale **2.5**, **−60.5 m**) on heliports. One row each, with a height offset (± m from 0; helipad offset is added to −60.5 m). Both use XREF `[direction]` from helipad heading (0 = heading 90°).

### Changed

- Tower dummy XREF is **`container`** at scale **`0.0001`** (FSG and FS4). `landrider01` was too small or unsupported on FSG mobile; Open still reads it.
- Coordinate field captions recommend **Google Earth decimal**. Set Google Earth **Settings** to decimal; DMS from Earth is rounded and positions are inaccurate.
- Airport edit screen uses a two-column field layout (heliport and pushback); phone portrait stacks the fields.

### Notes

- Aerofly uses only **one** extra airport mod per ICAO at a time. Other packages for the same airport are ignored.

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

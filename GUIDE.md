# Pushback Creator App — user guide

This guide is for scenery authors who already use Aerofly **FSG** (Android) and/or **FS4** (PC). The app UI is English.

## 1. Start or open a project

On the home screen:

| Button | Meaning |
| --- | --- |
| **New project** | Empty project. Suggested package name: `pca_scenery_pushback`. |
| **Open TME (FSG)** | Load an existing FSG `.tme` (including packages this app did not create). |
| **Open ZIP (FS4)** | Load an FS4 scenery zip (`package\airports\…`). |

On Android, **Open** uses the system file picker. On **Windows** and **Mac**, **Export** asks for a **folder** (Downloads, Documents, …) and writes `{package}.tme` / `.zip` / `.kml` into it (replace if that name already exists).

## 2. Project (airport list)

This is the second screen.

1. Set **Project package name** — this becomes the first folder inside the TME/ZIP. Use letters, digits, and underscores; keep it stable if you already copied files into the sim.
2. **New airport** — choose **Pushback airport (ICAO + 00)** or **Heliport (4-character code)**.
3. **Add TME (FSG)** / **Add ZIP (FS4)** — merge more airports into **this** project. Airports with a TME code that is already in the list are skipped. The package name is not replaced.
4. **Edit** / **Delete** — change or remove one airport.
5. **Sort A–Z** — order the list by ICAO (TME code). Open, add, and new airports keep their current order until you press this.
6. Export:
   - **Export TME (FSG)** — FSG
   - **Export ZIP (FS4)** — PC
   - **Export KML** — Google Earth (`{package}.kml`); does not replace a TME/ZIP export
7. **Quit project** — leave the project. If you changed something and did not export TME or ZIP, the app asks before closing.

**Exit** the whole app from the flyout (hamburger) menu, not from this screen.

## 3. Airport

| Field | Pushback airport | Heliport |
| --- | --- | --- |
| ICAO | Exactly 4 letters or digits. Folders/files: `ICAO` + `00`. | Exactly 4 characters (may be unofficial). |
| Name `[sname]` | Optional map title (max 32). Empty → TME code (`ICAO00`). | Optional. Shown on the Location map above the ICAO. Empty → the 4-character code. |
| Helipad position | Required. Also used as the dummy airport position. | Required. |
| Helipad radius | Metres in TSC/WAD `[radius]`. Default **20**. Drawn as the map circle. | Same. |
| Helipad heading | Degrees, default 0. | Same. |
| Tower position shift | Optional. Must not match the helipad or a parking slot (no overlay). Empty = no shift. | Optional, same rules (must not match the helipad). |
| Tower height AGL | Optional. **0** = default (no dummy). Any other height is written in a **POI** `.toc` (`container`), not next to the airport files. | Same (POI `.toc`). |
| Windsock | Not used. | Optional position. Empty = none. Exported in a **POI** (terrain autoheight). XREF **direction** (deg, 0 = east) and **scale factor** (default 1), then height offset m relative to 0. |
| Helipad object | Not used. | Optional checkbox. Same lon/lat as the helipad. Height offset m is added to **−60.5 m**. Written in the same heliport **POI**. |
| Parking slots | Add at least one pushback slot for a useful package. **Sort A–Z** orders by name. | Not used. |

**Save** keeps the airport in the project. **Cancel** (or the Shell back arrow) discards unsaved field changes; a new airport that was never saved is removed from the list.

## 4. Coordinates (map, OSM search, or paste)

The airport screen is a **map workspace** (same split as TGI Model Transformer: inspector left / map right on a wide screen, inspector top / map bottom on a phone).

1. If the airport has **no coordinates yet**, **Find location (OpenStreetMap)** is shown: search for the airport name or ICAO, pick a result to jump the map, then **tap the map** to place the **helipad** (airport location). After that the search panel hides.
2. Other objects (tower, windsock, pushback slots, later XREF) are available only after the helipad is set.
3. **Drag** the footprint or the round handle to move. Drag the **diamond** at the arrow tip to set **heading** (0 = north, clockwise) for helipad, parking, and windsock. Helipad heading and windsock **direction** are written back to the inspector. The **tower** is a point with a grab ring (no heading). Selecting an object in the **list** pans the map to it; selecting it **on the map** keeps the current view.
4. Helipad overlay is a **circle** using the helipad radius (default 20 m). Pushback overlays are **rectangles** from parking size. The windsock is a placeholder square (no real XREF footprint yet) rotated from its **direction**; **scale factor** sizes that square. You can still paste **decimal** coordinates into the inspector fields.

Imagery tiles are **Esri World Imagery** (XYZ, no app API key). Switch the layer control to **Streets** (OpenStreetMap) if you need labels. Zoom with **pinch** (two fingers) or the map **+ / −**. A tablet usually does not need extra zoom. On a small phone you can go up to level **21** (tiles stay sharp to 18, then enlarge). On **Windows**, the mouse wheel zooms. An internet connection is required for tiles and OSM search.

Google Earth is optional (KML export, or paste instead of the map):

1. **Export KML** from the project screen and open it in Google Earth.
2. To paste coordinates instead of using the map: in Google Earth **Settings**, set the coordinate format to **decimal**, then copy/paste into the inspector.

Accepted paste format (decimal only): `40.4675, 50.0539` or `40.4675 50.0539` (latitude first). If the first number is outside ±90 it is treated as TSC lon, lat.

Heading is degrees in the Aerofly sense (0 = north). Parking size defaults to **35**. Helipad radius defaults to **20**. Windsock XREF **direction** 0 is east; **scale factor** defaults to **1**. The dummy helipad object (`airportmast03`) still uses helipad heading and a fixed scale of **2.5**.

**A helipad is currently required** so the extra pushback parking spaces appear in Aerofly FS (FSG / FS4). That applies to dummy pushback airports as well as heliports.

## 5. Parking slot

For each pushback stand:

1. Paste or type **position**.
2. Set **heading** (default 0) and **size** (default 35).
3. Set **name** (gate), for example `A1`. New slots are suggested as `A1`, `A2`, …
4. On the airport screen, **Sort A–Z** orders the parking list by name. New slots stay at the end until you sort again.

**Save** / **Cancel** work like on the airport screen.

## 6. Put files into the simulator

### FSG Mobile

1. Export **TME (FSG)**.
2. On Android, the file is in **Downloads** (a file with the same name is overwritten).
3. Copy `*.tme` into the FSG extra-content folder. A file manager that can see app data, or USB from a PC, is typical. Path shape:

   `Android/data/com.aerofly.aeroflyfsg1/files/`

4. Restart FSG. Find the place on the location map (dummy codes look like `EPWA` in the sim; the files are still `epwa00`). XREF objects (windsock, helipad object, tower dummy) live in the TME under `scenery/poi/` with an ETC2 `pca_anchor.ttx`.

### Aerofly FS 4 PC

1. Export **ZIP (FS4)**.
2. Unpack into:

   `Documents\Aerofly FS 4\addons\scenery\`

   so `{package}\airports\{code}\` appears under `addons\scenery`. XREF objects (heliport windsock / dummy helipad / tower dummy, and pushback tower dummy) are in `{package}\poi\{e#####n####}_pca_{code}\` with a buried `pca_anchor.tmb` / `pca_anchor.ttx` (ETC2 in the TME, DXT1 in the FS4 ZIP) so autoheight can use terrain. Without those objects there is no `poi` folder.
3. Restart FS4.

**One airport mod per ICAO:** Aerofly loads only **one** extra airport package for a given ICAO code at a time (for example one `EPWA00`). A second TME or scenery folder for the same airport is ignored and does not appear. Keep a single package for that ICAO, or merge airports into one project with **Add TME / Add ZIP** and export once.

## 7. Google Earth (optional)

Map positions in the app are already accurate enough for scenery work. Google Earth is optional: to **look at** the project, or as an **alternative to the map** if you prefer to move markers there and paste coordinates back.

**Before you copy any coordinates:** in Google Earth **Settings**, set the coordinate format to **decimal**. The app accepts **decimal only** (`40.4675, 50.0539` or `40.4675 50.0539`, latitude first). Degrees/minutes/seconds are not parsed. Google Earth DMS is rounded and would shift positions.

1. **Export KML** on the project screen and open `{package}.kml` in Google Earth. Each airport is a folder (**Helipad H01**, **Tower** / **Windsock** when set, **Pushback …**).
2. To change a point without using the in-app map: move the marker in Earth, copy the **decimal** coordinates into the matching inspector field, then **Save**.
3. If you also need the files in the simulator, export **TME (FSG)** / **ZIP (FS4)** again.

You can use your own named Google Earth markers the same way, without KML. If the first pasted number is outside ±90 it is treated as TSC lon, lat.

## 8. Tips

- One project can hold many **different** airports. Use **Add TME / Add ZIP** to combine packages, then export once.
- Do not install two mods for the **same** ICAO at once; only one is used.
- Re-export **replaces** TSC/WAD (XREF objects as a **POI** under `scenery/poi/`) with this app’s layout. Import still reads a legacy airport `.toc` and POI `.toc`: `[sname]`, pushback-tagged parking, `tower_position`, helipad `[radius]`, `container` (and legacy `landrider01`), `airport_windsock_large1` (including `[direction]` and `[scale_factor]`), and `airportmast03`.
- If Play Protect asks to scan a new APK, wait for the scan. A previous version of the app does not skip the scan for a new file.

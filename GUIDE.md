# Pushback Creator App — user guide

This guide is for scenery authors who already use Aerofly **FSG** (Android) and/or **FS4** (PC). The app UI is English.

## 1. Start or open a project

On the home screen:

| Button | Meaning |
| --- | --- |
| **New** | Empty project. Suggested package name: `pca_scenery_pushback`. |
| **Open TME (FSG)** | Load an existing FSG `.tme` (including packages this app did not create). |
| **Open ZIP (FS4)** | Load an FS4 scenery zip (`package\airports\…`). |

On Android, **Open** uses the system file picker. On Windows, use the file dialog.

## 2. Project (airport list)

This is the second screen.

1. Set **Project package name** — this becomes the first folder inside the TME/ZIP. Use letters, digits, and underscores; keep it stable if you already copied files into the sim.
2. **New** — choose **Pushback airport (ICAO + 00)** or **Heliport (4-character code)**.
3. **Add TME (FSG)** / **Add ZIP (FS4)** — merge more airports into **this** project. Airports with a TME code that is already in the list are skipped. The package name is not replaced.
4. **Edit** / **Delete** — change or remove one airport.
5. Export:
   - **Export TME** — FSG
   - **Export ZIP (FS4)** — PC
   - **Export KML** — Google Earth (`{package}.kml`); does not replace a TME/ZIP export
6. **Quit project** — leave the project. If you changed something and did not export TME or ZIP, the app asks before closing.

**Exit** the whole app from the flyout (hamburger) menu, not from this screen.

## 3. Airport

| Field | Pushback airport | Heliport |
| --- | --- | --- |
| ICAO | Exactly 4 letters or digits. Folders/files: `ICAO` + `00`. | Exactly 4 characters (may be unofficial). |
| Name `[sname]` | Optional map title (max 32). Empty → TME code (`ICAO00`). | Optional. Shown on the Location map above the ICAO. Empty → the 4-character code. |
| Helipad position | Required. Also used as the dummy airport position. | Required. |
| Helipad heading | Degrees, default 0. | Same. |
| Parking slots | Add at least one pushback slot for a useful package. | Not used (button disabled). |

**Save** keeps the airport in the project. **Cancel** (or the Shell back arrow) discards unsaved field changes; a new airport that was never saved is removed from the list.

## 4. Coordinates (Google Earth is optional)

You can type coordinates from **any source**, or use **Google Earth** to place markers and copy values.

Google Earth (optional):

1. Open the airport (or helipad).
2. Place a marker on the **helipad** and on each **pushback** stop.
3. Unique names (`EPWA-H01`, `EPWA-A1`, …) make it easier to correct one point later.
4. Copy coordinates into the app.

Accepted formats:

- Decimal: `40.4675, 50.0539` or `40.4675 50.0539` (latitude first)
- Google Earth DMS: `40°28'02.88"N 50°03'13.86"E` (space or comma between the two values)

Heading is degrees in the Aerofly sense. Parking size defaults to **35**. Helipad radius in the files is always **20**.

**A helipad is currently required** so the extra pushback parking spaces appear in Aerofly FS (FSG / FS4). That applies to dummy pushback airports as well as heliports.

## 5. Parking slot

For each pushback stand:

1. Paste or type **position**.
2. Set **heading** (default 0) and **size** (default 35).
3. Set **name** (gate), for example `A1`. New slots are suggested as `A1`, `A2`, …

**Save** / **Cancel** work like on the airport screen.

## 6. Put files into the simulator

### FSG Mobile

1. Export **TME**.
2. On Android, the file is in **Downloads** (a file with the same name is overwritten).
3. Copy `*.tme` into the FSG extra-content folder. A file manager that can see app data, or USB from a PC, is typical. Path shape:

   `Android/data/com.aerofly.aeroflyfsg1/files/`

4. Restart FSG. Find the place on the location map (dummy codes look like `EPWA` in the sim; the files are still `epwa00`).

### Aerofly FS 4 PC

1. Export **ZIP (FS4)**.
2. Unpack into:

   `Documents\Aerofly FS 4\addons\scenery\`

   so the folder `{package}\airports\{code}\` appears under `addons\scenery`.
3. Restart FS4.

## 7. Correct positions in Google Earth

After a first export you can refine points:

1. **Export KML** and open it in Google Earth (folders per airport: **Helipad H01** and **Pushback …**).
2. Move the markers, copy the new coordinates back into the matching airport or parking fields, then **Save**.
3. Export TME / ZIP again and copy the files into the simulator.

You can also keep using your own named Google Earth markers the same way, without KML.

## 8. Tips

- One project can hold many airports. Use **Add TME / Add ZIP** to combine packages, then export once.
- Re-export **replaces** TSC/WAD with this app’s layout. Import still reads `[sname]` and pushback-tagged parking.
- If Play Protect asks to scan a new APK, wait for the scan. A previous version of the app does not skip the scan for a new file.
- Do not raise the helipad radius in a hex editor expecting the app to keep it: export always writes **20**.

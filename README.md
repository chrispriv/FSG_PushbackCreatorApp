# Pushback Creator App

A .NET MAUI 9 app to add extra pushback parking slots and helipads for Aerofly FSG Mobile and FS4 PC. User interface, tutorials, and changelogs stay in English.

**Version:** 1.0.0 TEST

## What this build does

Four screens:

1. **Home** — **New**, **Open TME (FSG)**, **Open ZIP (FS4)**.
2. **Airports** — project package name, list, new pushback airport or heliport, edit, delete, **Export TME**, **Export ZIP (FS4)**.
3. **Airport** — 4-character code (pushback airports become `ICAO` + `00`, heliports stay 4 characters), heliport name on the Location map (max 32), helipad position/heading, parking list. Heliports cannot add parking slots. Helipad radius is always 20 in the TSC/WAD (not shown in the form).
4. **Parking slot** — position, heading (default 0), size (default 35), gate name (suggested `A1`, `A2`, …).

Helipad position is also the dummy pushback airport position. A dummy pushback airport needs that helipad or FSG will not show the extra parking.

**Export ZIP (FS4)** writes a normal Windows zip: `package\airports\{code}\`. Unpack that folder into Aerofly FS4 `addons\scenery\`.

**Export TME** writes an Info-ZIP **Store** archive with Unix metadata (same style as FSG-readable packages). The first folder inside the TME is the project package name (editable on the airport list; **New** suggests `pca_scenery_pushback`, Open fills it from the file). Dummy pushback airports write `[sname]` as ICAO + `00`. Heliports write the same map name in `[sname]` (Location map) and `[lname]` (Change Location dialog), max 32 characters.

WAD conversion (CalcES RAD mode, west/south negative):

- Longitude: `65536 × (0.5 + 0.5 × lon / 180)`
- Latitude: `65536 × (0.5 + 0.5 × (tan(2.3311223704144 × lat / 180) / 2.3311223704144))` with `tan` in radians
- Heading below or equal 90°: `(90 − hdg) × π / 180`
- Heading above 90°: `(450 − hdg) × π / 180`

Paste Google Earth **decimal** (`lat, lon` or `lat lon`) or **DMS** (`40°28'02.88"N 50°03'13.86"E`, space or comma). Values are stored as decimal. TSC `lon lat` is still accepted when the first number is outside ±90.

Airport and parking screens use **Save** / **Cancel** at the bottom. The mobile Shell back arrow uses the same Cancel path, including the unsaved-changes warning. Cancel on the airport list closes the project and warns if changes were not exported as a TME file. **Export TME** writes the Store ZIP. **Open TME** reads `.tsc` entries (degrees); `.wad` is regenerated on the next export.

## Open in Visual Studio 2022

1. Open `FSG_PushbackCreatorApp.sln`.
2. In the debug toolbar, choose **Windows Machine** (target `net9.0-windows`).
3. Press **F5**.
4. Flyout (hamburger): Home, Exit.

**Breakpoints:** set them on a statement inside a `.xaml.cs` method body, not on XAML `Clicked=`. XAML event handlers often show 0 CodeLens references; that is normal.

## Targets

| Platform | Artifact | Where to build |
| --- | --- | --- |
| Windows | unpackaged WinUI app | This PC (Visual Studio 2022) |
| Android | APK | This PC (Visual Studio 2022 + Android SDK) |
| iOS | IPA | Later, on the MacBook (Xcode + Pair to Mac) |
| Mac | Mac Catalyst app | Later, on the MacBook |

On Android, Open uses the system file picker. Export writes to Downloads and replaces a file with the same name (the project package name is not changed by Android “ (1)” copies). Copy the exported `.tme` into the FSG extra-content folder with USB or a file manager that can open `Android/data/com.aerofly.aeroflyfsg1/files/`. Unpack an FS4 ZIP into `Documents\Aerofly FS 4\addons\scenery`.

## Run from the command line (Windows)

```bash
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0
```

Application id: `com.chrispriv.fsgpushbackcreator`

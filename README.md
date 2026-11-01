# Pushback Creator App

A .NET MAUI 9 app to add **extra pushback parking** and **helipads** for [Aerofly FS Global](https://www.aerofly.com/) (FSG, Android) and **Aerofly FS 4** (PC).

The same project can be exported as:

- **TME** — FSG extra-content package (Unix Store zip)
- **ZIP (FS4)** — scenery zip to unpack into `addons\scenery`
- **KML** — Google Earth check of all helipad and pushback positions

Current version: **1.3.0** (in progress, local). Android package id `com.chrispriv.fsgpushbackcreator`. Public GitHub release is still **1.2.0**.

| Document | Contents |
| --- | --- |
| [GUIDE.md](GUIDE.md) | Step-by-step use, coordinates, install into FSG / FS4 |
| [CHANGELOG.md](CHANGELOG.md) | What changed in each version |

UI, this README, the guide, and the changelog are in **English**.

## What it does

Aerofly already has parking at many airports. This app writes **additional** dummy airports and heliports so you can add:

- **Pushback slots** next to an existing FSG/FS4 airport (folder/file names `ICAO` + `00`, for example `EPWA00`)
- **Standalone heliports** with a 4-character code (official ICAO or a private code such as `CH99`)

Each place needs a **helipad** (radius is always **20** in the files). Without that helipad, FSG will not show the extra parking.

You enter positions as **decimal or DMS** (from Google Earth or any other source). The app stores decimal coordinates and writes Aerofly `.tsc` / `.wad` files. A helipad is currently required so extra pushback parking appears in Aerofly.

## Download

Windows (portable) and Android (APK) builds are on **[Releases](https://github.com/chrispriv/FSG_PushbackCreatorApp/releases)**.

- **Android:** sideload `PushbackCreatorApp-1.2.0.apk` (arm, arm64, and x64 in one package). Allow install from the file manager. Google Play Protect may scan a **new** APK once; that scan is per file, not a Play Store listing.
- **Windows:** download the portable zip, unpack it, run `FSG_PushbackCreatorApp.exe`. 64-bit Windows 10 (1809 or later) / Windows 11. No installer and no extra .NET install. Not the Microsoft Store.
- **iOS / Mac:** not in the GitHub release packages yet. Build Mac Catalyst first (run on the Mac via Pair to Mac), then an iOS IPA later. Same bundle id as Android (`com.chrispriv.fsgpushbackcreator`) so a free Apple Personal Team only uses **one** App ID for Mac and iPad.

## Quick start

1. **New**, or **Open TME / Open ZIP** to continue a package.
2. Set the **project package name** (folder inside the TME/ZIP, default `pca_scenery_pushback`).
3. **New** → pushback airport or heliport → 4-character ICAO, optional map name, helipad position and heading.
4. Enter coordinates yourself, or optionally copy them from Google Earth markers (decimal or DMS).
5. For pushback airports: **New parking slot** for each gate (position, heading, size, name).
6. **Export TME** for FSG and/or **Export ZIP (FS4)** for the PC. **Export KML** is optional, to review or move points in Google Earth.
7. Copy the TME into FSG extra content, or unpack the FS4 zip into `addons\scenery`. Details: [GUIDE.md](GUIDE.md).

**Quit** is only in the flyout menu (**Exit**). Airport and parking screens use **Save** / **Cancel**. The airport list uses **Quit project**.

## Install the scenery

**FSG (Android):** copy the `.tme` into the FSG extra-content folder (USB or a file manager that can open app data), typically under:

`Android/data/com.aerofly.aeroflyfsg1/files/`

**FS4 (PC):** unpack the zip so you get:

`Documents\Aerofly FS 4\addons\scenery\{package}\airports\{code}\`

Restart the simulator after copying files.

## Build from source (Windows PC)

Visual Studio 2022 with the .NET MAUI workload.

1. Open `FSG_PushbackCreatorApp.sln`.
2. Toolbar: **Debug** (not Release) and **Windows Machine**.
3. Press **F5**. Flyout: Home, **Exit**.

Command line:

```bash
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0
```

Sideloadable Android APK (Release, arm + arm64 + x64, assemblies embedded):

```bash
dotnet publish -f net9.0-android -c Release
```

The signed APK is `bin\Release\net9.0-android\com.chrispriv.fsgpushbackcreator-Signed.apk`.

Breakpoints: set them inside a `.xaml.cs` method body, not on XAML `Clicked=`.

## Mac (Pair to Mac) — before iOS IPA

Build **Mac Catalyst** first (`net9.0-maccatalyst`). The app runs **on the Mac**, not in the Windows debugger window. An iPad IPA is a later step (`net9.0-ios`).

On the Mac (once):

1. Xcode installed, opened once, license accepted. Keep it signed in with the same Apple ID as on the iPad Hello World app.
2. Install the .NET 9 SDK, then: `dotnet workload install maui` (includes `maccatalyst` and `ios`). Do **not** install Visual Studio for Mac.
3. System Settings → General → Sharing → **Remote Login** (SSH) on, so Visual Studio on Windows can pair.
4. Same Wi-Fi/LAN as the Windows PC.

Visual Studio 2022 on Windows **cannot F5 Mac Catalyst**. Pair to Mac is for **iOS** (iPad / simulator). The Mac app must be started **on the Mac**.

1. Copy `FSG_PushbackCreatorApp` onto the MacBook (USB, AirDrop, or a shared folder).
2. On the Mac, in Terminal:

```bash
cd ~/path/to/FSG_PushbackCreatorApp
dotnet restore
dotnet build -t:Run -f net9.0-maccatalyst
```

Or open the same folder in Visual Studio Code on the Mac (C# Dev Kit + .NET MAUI), pick the macOS / Mac Catalyst debug target, then F5 there. Signing: Automatic / Personal Team (same team as the iPad Hello World app).

Pair to Mac from Windows stays useful for the later **iOS** step: framework `net9.0-ios`, target the registered iPad. Same bundle id — do not create a second App ID.

## Technical notes

These details matter if you compare files with other Aerofly tools.

- Dummy pushback **folders and file names** stay `{icao}00`. TSC/WAD **`[icao]`** is the four-character code plus **two spaces** (for example `[EPWA  ]`) so Aerofly labels the place as `EPWA`. Heliports stay four characters.
- Optional **`[sname]`** (max 32). Empty input uses the TME code. Heliports also write **`[lname]`** with the same name.
- Pushback airports may set optional **`[tower_position]`** in both **TSC** (lon/lat) and **WAD** (world units, same conversion as other positions). Tower height AGL **0** skips the dummy `.toc`. Any other height writes `{icao}00.toc` (`landrider01`, scale `0.001`) next to the `.tsc`/`.wad` and a `cultivation_files` link with **`[auto_height][true]`** and **`[use_height_offset][true]`**. Place-level **`[autoheight][true]`** is always written. Tower coordinates must not match the helipad or a parking slot. **Export KML** includes a **Tower** pin when a tower position is set.
- **Open / Add TME or ZIP:** ICAO from the airport folder or `.tsc` file name (first four characters). Kind is **pushback** if any parking block has `[tags][pushback]`; otherwise **heliport**. The next export always uses this app’s layout. `.wad` is regenerated.
- **Add TME / Add ZIP** merges airports into the current project; duplicate TME codes are skipped. Package name stays as it is.
- **Sort A–Z** on the airport list orders airports by ICAO (TME code). Open TME/ZIP keeps archive order; new or merged airports stay at the end until you sort.
- **Export KML** does not count as “project exported”; **Quit project** still warns until you export TME or ZIP.
- On Android, export writes to **Downloads** and **replaces** a file with the same name (no `name (1).tme` from the project package name).
- WAD conversion (CalcES RAD, west/south negative):
  - Longitude: `65536 × (0.5 + 0.5 × lon / 180)`
  - Latitude: `65536 × (0.5 + 0.5 × (tan(2.3311223704144 × lat / 180) / 2.3311223704144))` with `tan` in radians
  - Heading ≤ 90°: `(90 − hdg) × π / 180`
  - Heading > 90°: `(450 − hdg) × π / 180`

## License

[MIT](LICENSE) — Copyright (c) 2026 chrispriv.

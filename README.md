# Laptop Backlight Control for Lenovo

A small Windows app that lets you **detect, control, and automate the keyboard backlight** on Lenovo laptops by reading and writing the Embedded Controller (EC) register responsible for backlight state. Available in **English and Chinese**.

> **⚠ Important — Windows Defender / antivirus will flag the driver.**
> This app uses the **WinRing0** kernel driver for low-level EC access. Windows Defender flags WinRing0 as a *vulnerable driver* and **blocks or quarantines it** (you'll see `StartService` error **225**). This is expected for this class of tool — it is not malware. The app includes a one-click fix: **Settings → "Allow driver (add Defender exclusion)"**, which you must use once (it's also applied automatically when you enable *Apply on restart*). Without the exclusion the backlight features won't work. The app must run **as Administrator** with **Memory Integrity (Core Isolation) OFF**.

> **⚠ Disclaimer:** This is an independent, third-party utility. It is **not** affiliated with, endorsed by, or associated with Lenovo Group Ltd. "Lenovo" is a registered trademark of Lenovo Group Ltd. Use at your own risk — see [Disclaimer](#disclaimer) below.

---

## Why this exists

Consumer Lenovo laptops (e.g. the Yoga Slim series) have no public software API for the keyboard backlight. The Fn+Space toggle is handled entirely inside the EC and emits no OS-level key event, and Lenovo Vantage reaches the EC through an undocumented internal pipeline. This app solves the problem by finding and writing the EC register directly — so you can, for example, force the backlight **Off** automatically every time the computer starts.

## Features

| Tab | What it does |
|-----|-------------|
| **Control** | Apply any backlight stage (Off / Dim / Bright / Auto or custom) with one click |
| **Detection** | Guides you through capturing EC readings in each state, finds the controlling register automatically, and lets you verify it live |
| **Settings** | **Apply on restart** (set a stage automatically at logon), language selector (English / Chinese), Windows Defender driver exclusion, manual register override, and add/rename/delete stages |
| **About** | Author info, disclaimer, third-party credits |

## Requirements

- Windows 10 / 11 (x64)
- **Administrator rights** — required to load the kernel EC driver (WinRing0)
- **Memory Integrity (Core Isolation / HVCI) must be OFF** — the WinRing0 kernel driver will not load if it is enabled
- A **Windows Defender exclusion** for the driver — Defender flags WinRing0 as a "vulnerable driver" and blocks it. The app can add the exclusion for you (Settings → *Allow driver*), which is also done automatically when you enable *Apply on restart*.

The released build is **self-contained** — no .NET runtime install is required.

## Installation

1. Download `LaptopBacklightControl-v1.0.0-win-x64.zip` from [Releases](https://github.com/mcyikhei/laptop-backlight-control-lenovo/releases).
2. Extract the ZIP. It contains a folder with `LaptopBacklightControl.exe`, the `native\` driver, and `readme.txt`.
3. Right-click `LaptopBacklightControl.exe` → **Run as administrator**.
4. Go to **Detection** and follow the steps to find your register (≈ 2 minutes). Skip any backlight level your keyboard doesn't have.
5. Use **Control** to set the backlight, or **Settings → Apply on restart** to set a stage automatically at every logon.

## How detection works

Detection reads all 256 EC registers in each backlight state you set, then finds registers that are **stable within each state but differ across states** — that uniquely identifies the backlight control byte. It is read-only during detection; writes only happen during the optional Verify step and when you explicitly apply a stage.

**Verified on:** Lenovo Yoga Slim 13s ACN 2021 (type 82CY) — register `0xAE`, values: Auto=`0x33`, Bright=`0x23`, Dim=`0x13`, Off=`0x03`. Other models discover their own register via Detection (none is hardcoded).

## Building from source

```powershell
# Requires the .NET 8 SDK
git clone https://github.com/mcyikhei/laptop-backlight-control-lenovo.git
cd laptop-backlight-control-lenovo
dotnet build src\LenovoLaptopBacklight\LenovoLaptopBacklight.csproj -c Release

# Self-contained single-file publish (no runtime needed on the target):
dotnet publish src\LenovoLaptopBacklight\LenovoLaptopBacklight.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
```

The WinRing0 driver (`src\LenovoLaptopBacklight\native\WinRing0x64.sys`) is bundled next to the exe.

## Config file

`%ProgramData%\LenovoLaptopBacklight\config.json` — shared between the UI and the logon "apply on restart" task. A run log is written next to it (`backlight.log`).

## Disclaimer

This software is an independent, third-party utility and is **NOT** affiliated with, endorsed by, or associated with Lenovo Group Ltd. or any of its subsidiaries. "Lenovo" is a registered trademark of Lenovo Group Ltd.

This software directly accesses low-level hardware (the Embedded Controller) via a kernel-mode driver. The author accepts no liability for any damage, data loss, warranty issues, or other consequences arising from the use of this software.

## Credits & Third-Party Software

- **WinRing0 (OpenLibSys)** — kernel-mode I/O port access driver, BSD license. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
- **NoteBook FanControl / ec-probe** (hirschmann/nbfc) — EC access approach and register-detection technique, GPL-3.0.
- **ACPI Specification §12.9** — EC read/write protocol.

## Author

mcyikhei — https://github.com/mcyikhei

## License

MIT — see [LICENSE](LICENSE).

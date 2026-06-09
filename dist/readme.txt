===========================================================================
 Laptop Backlight Control for Lenovo  —  v1.0.0
 https://github.com/mcyikhei/laptop-backlight-control-lenovo
===========================================================================

A small tool to detect, control, and automate the keyboard backlight on
Lenovo laptops. Available in English and Chinese.

***************************************************************************
 IMPORTANT - WINDOWS DEFENDER / ANTIVIRUS WILL FLAG THE DRIVER
***************************************************************************
 This app uses the WinRing0 kernel driver for low-level EC access. Windows
 Defender flags WinRing0 as a "vulnerable driver" and will BLOCK or
 QUARANTINE it (you will see "StartService error 225"). This is EXPECTED
 for this class of tool -- it is not malware.

 The fix is built in:  open the "Settings" tab and click
     "Allow driver (add Defender exclusion)".
 You must do this once, or the backlight features will not work. (It is
 also applied automatically when you enable "Apply on restart".)

 The app must run AS ADMINISTRATOR with Memory Integrity (Core Isolation)
 turned OFF.
***************************************************************************

---------------------------------------------------------------------------
 DISCLAIMER
---------------------------------------------------------------------------
This is an independent, THIRD-PARTY utility. It is NOT affiliated with,
endorsed by, or associated with Lenovo Group Ltd. "Lenovo" is a registered
trademark of Lenovo Group Ltd.

This software accesses low-level hardware (the Embedded Controller) through
a kernel driver. Use at your own risk. The author accepts no liability for
any damage, data loss, or warranty issues arising from its use.

---------------------------------------------------------------------------
 WHAT'S IN THIS FOLDER
---------------------------------------------------------------------------
  LaptopBacklightControl.exe   The application (self-contained, no install)
  native\WinRing0x64.sys       The kernel driver it uses (keep next to .exe)
  readme.txt                   This file

Keep these together. Do not move the .exe out of this folder on its own.

---------------------------------------------------------------------------
 REQUIREMENTS
---------------------------------------------------------------------------
  * Windows 10 / 11 (64-bit)
  * Must be run AS ADMINISTRATOR (it loads a kernel driver)
  * "Core Isolation > Memory Integrity" must be OFF
      (Windows Security > Device security > Core isolation)
  * Windows Defender exclusion for the driver (see the notice above).

---------------------------------------------------------------------------
 HOW TO USE
---------------------------------------------------------------------------
  1. Right-click  LaptopBacklightControl.exe  ->  "Run as administrator".

  2. Go to the "Settings" tab and click
        "Allow driver (add Defender exclusion)".

  3. Go to the "Detection" tab and click "Start Detection".
        - For each level (Off / Dim / Bright / Auto), set your keyboard
          backlight to that level with Fn+Space, then click "Capture".
        - If your keyboard does not have a level, click "Skip this level".
        - Click "Analyse", then "Verify" to watch the backlight cycle,
          then "Save".

  4. "Control" tab: click a stage (e.g. Off) to apply it instantly.

  5. To turn the backlight off (or any stage) automatically at every
     startup: "Settings" tab -> tick "On restart, set backlight to:" and
     choose the stage. It runs at logon, in your session, with no UAC pop-up.

---------------------------------------------------------------------------
 LANGUAGE
---------------------------------------------------------------------------
  Settings -> Language -> English / Chinese  (applied instantly, remembered).

---------------------------------------------------------------------------
 NOTES / TROUBLESHOOTING
---------------------------------------------------------------------------
  * "Windows Defender is blocking the WinRing0 driver" -> do step 2 above.
  * Config + log are stored in:
        %ProgramData%\LenovoLaptopBacklight\
  * Verified on Lenovo Yoga Slim 13s ACN 2021 (type 82CY). Other models are
    supported via Detection (the register is found per-machine).

License: MIT.  Driver: WinRing0 (OpenLibSys, BSD). See the project page.
===========================================================================

# Third-Party Notices

This software bundles or builds on the following third-party open-source components.

---

## WinRing0 (OpenLibSys)

The `WinRing0x64.sys` kernel driver bundled in this repository is from the WinRing0 project.

- **Project:** WinRing0 / OpenLibSys
- **Repository:** https://github.com/GermanAizek/WinRing0 (maintained fork)
- **Original author:** Noriyuki Miyazaki (openlibsys.org)
- **License:** BSD-style (see below)

```
Copyright (c) 2007-2009 OpenLibSys.org. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:
1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

---

## NoteBook FanControl / ec-probe

The Embedded Controller (EC) access approach, ACPI EC port protocol, and register-detection
methodology implemented in this software are inspired by the NoteBook FanControl project.

- **Project:** NoteBook FanControl (NBFC)
- **Repository:** https://github.com/hirschmann/nbfc
- **Author:** Stefan Hirschmann
- **License:** GPL-3.0
- **Note:** No source code from NBFC is included in this repository. Only the general
  technique of reading/writing EC RAM via I/O ports 0x62/0x66 was used as prior art.

---

## ACPI Specification

The EC read/write protocol (ACPI §12.9) is from the publicly available
ACPI Specification published by the UEFI Forum: https://uefi.org/specifications

---

## Disclaimer

"Lenovo" is a registered trademark of Lenovo Group Ltd. This software is an independent,
third-party utility and is NOT affiliated with, endorsed by, or associated with
Lenovo Group Ltd. or any of its subsidiaries.

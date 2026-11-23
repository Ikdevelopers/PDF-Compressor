# Third-Party Notices & Licensing

PDF Compressor incorporates several open-source software packages and libraries. This document outlines the third-party components and their respective licensing terms.

---

## 1. Artifex Ghostscript
- **Component**: Ghostscript (10.08.0)
- **Copyright**: (C) 2001-2026 Artifex Software, Inc.
- **License**: GNU General Public License v3 (GPLv3)
- **Source Code**: https://github.com/ArtifexSoftware/ghostpdl-downloads
- **Usage**: Used as an independent external sub-process for document rasterization, downsampling, and JPEG re-encoding. No direct linking or derivation. Users may replace or upgrade the binary in the `Tools/gs-*` directory.

---

## 2. QPDF
- **Component**: QPDF (12.4.2)
- **Copyright**: (C) 2005-2026 Jay Berkenbilt
- **License**: Apache License 2.0
- **Source Code**: https://github.com/qpdf/qpdf
- **Usage**: Invoked as a stand-alone command-line executable for PDF object stream optimization, validation, and Fast Web View linearization.

---

## 3. Microsoft.Data.Sqlite
- **Component**: Microsoft.Data.Sqlite & SQLitePCLRaw
- **Copyright**: (C) Microsoft Corporation. All rights reserved.
- **License**: MIT License
- **Repository**: https://github.com/dotnet/efcore
- **Usage**: Embedded local relational storage for compression history records.

---

## 4. SQLite
- **Component**: SQLite 3 Engine
- **License**: Public Domain (Dedicated to the public domain)
- **Website**: https://www.sqlite.org/copyright.html

---

## 5. Inno Setup
- **Component**: Inno Setup Compiler (6.7.3)
- **Copyright**: (C) 1997-2026 Jordan Russell, Martijn Laan
- **License**: Inno Setup License (Modified BSD-style)
- **Website**: https://www.innosetup.com
- **Usage**: Used to generate Windows installer executables.

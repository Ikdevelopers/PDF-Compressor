# Build Instructions – PDF Compressor

This document details how to build **PDF Compressor** from source code, execute automated test suites, and generate the release packages and Inno Setup installers.

---

## 📋 Prerequisites

1. **Operating System**: Windows 10 (version 1809+) or Windows 11 (x64 or x86).
2. **.NET 8 SDK**: .NET 8.0.400 or later (`dotnet --version`).
3. **Inno Setup**: Inno Setup 6.2+ (for generating installer executables). Ensure `ISCC.exe` is installed.
4. **Python 3.10+**: (Optional, used for helper asset generation scripts).

---

## 🏗️ Project Architecture

The solution `PDFCompressor.sln` contains 3 projects:
- `src/PDFCompressor.Core`: Business logic, engine coordination, Ghostscript & QPDF wrappers, SQLite history, and settings service.
- `src/PDFCompressor.App`: Native Windows Forms desktop application with modern Windows 11 styling.
- `tests/PDFCompressor.Tests`: Comprehensive xUnit test suite (unit and integration tests).

Native dependencies located in `Tools/`:
- `Tools/gs-x64`: Ghostscript 10.08.0 64-bit binaries (`gswin64c.exe`, `gsdll64.dll`).
- `Tools/gs-x86`: Ghostscript 10.08.0 32-bit binaries (`gswin32c.exe`, `gsdll32.dll`, `vcruntime140.dll`, `msvcp140.dll`).
- `Tools/qpdf-x64`: QPDF 12.4.2 64-bit binaries (`qpdf.exe`).
- `Tools/qpdf-x86`: QPDF 12.4.2 32-bit binaries (`qpdf.exe`).

---

## 🔨 Step-by-Step Build

### 1. Restore & Build the Solution
```powershell
dotnet restore PDFCompressor.sln
dotnet build PDFCompressor.sln -c Release
```

### 2. Run the Automated Tests
```powershell
dotnet test tests/PDFCompressor.Tests/PDFCompressor.Tests.csproj -c Release
```
Expected result: **21 passed, 0 failed, 0 skipped**.

### 3. Publish Self-Contained Binaries

#### For Windows 64-bit (x64):
```powershell
dotnet publish src/PDFCompressor.App/PDFCompressor.App.csproj -c Release -r win-x64 --self-contained true -o build/publish/x64
New-Item -ItemType Directory -Force -Path build/publish/x64/Tools
Copy-Item -Recurse -Force Tools/gs-x64 build/publish/x64/Tools/
Copy-Item -Recurse -Force Tools/qpdf-x64 build/publish/x64/Tools/
```

#### For Windows 32-bit (x86):
```powershell
dotnet publish src/PDFCompressor.App/PDFCompressor.App.csproj -c Release -r win-x86 --self-contained true -o build/publish/x86
New-Item -ItemType Directory -Force -Path build/publish/x86/Tools
Copy-Item -Recurse -Force Tools/gs-x86 build/publish/x86/Tools/
Copy-Item -Recurse -Force Tools/qpdf-x86 build/publish/x86/Tools/
```

---

## 📦 Generating Installers

Compile the Inno Setup script for each target architecture:

```powershell
# For x64 Setup:
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer-x64.iss

# For x86 Setup:
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer-x86.iss
```

Output:
- `Release/x64/PDFCompressor-Setup-x64.exe`
- `Release/x86/PDFCompressor-Setup-x86.exe`

---

## 🗜️ Creating Portable Zip Packages

Compress the published folder into a standard ZIP:
```powershell
Compress-Archive -Path "build\publish\x64\*" -DestinationPath "Release\Portable\PDFCompressor-v1.0.0-Portable-x64.zip" -Force
Compress-Archive -Path "build\publish\x86\*" -DestinationPath "Release\Portable\PDFCompressor-v1.0.0-Portable-x86.zip" -Force
```

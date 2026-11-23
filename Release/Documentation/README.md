# ⚡ PDF Compressor – Professional Native Windows Desktop Application

<p align="center">
  <img src="assets/app_icon.png" alt="PDF Compressor Logo" width="120" height="120" />
</p>

<h3 align="center">High-Performance, 100% Offline PDF Optimization for Windows 10 & 11</h3>

<p align="center">
  <a href="#-quick-download--install"><img src="https://img.shields.io/badge/Download-Latest%20Release-0078D7?style=for-the-badge&logo=windows&logoColor=white" alt="Download"></a>
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue?style=for-the-badge" alt="Windows 10/11">
  <img src="https://img.shields.io/badge/Framework-.NET%208.0%20WinForms-purple?style=for-the-badge" alt=".NET 8">
  <img src="https://img.shields.io/badge/Privacy-100%25%20Offline%20Local-10B981?style=for-the-badge" alt="Privacy">
</p>

---

## 🚀 Quick Download & Install

> [!IMPORTANT]
> **No installation required to get started!** Choose your Windows architecture below. Single-click to download and run locally on your PC.

### 📥 Direct One-Click Download Links

| Package | Architecture | Format | Download Link |
| :--- | :--- | :--- | :--- |
| **Windows 64-bit Installer (Recommended)** | 64-bit (x64) | `.exe` Setup | [**⬇️ Download PDFCompressor-Setup-x64.exe**](https://github.com/Ikdevelopers/PDF-Compressor/releases/latest/download/PDFCompressor-Setup-x64.exe) |
| **Windows 32-bit Installer** | 32-bit (x86) | `.exe` Setup | [**⬇️ Download PDFCompressor-Setup-x86.exe**](https://github.com/Ikdevelopers/PDF-Compressor/releases/latest/download/PDFCompressor-Setup-x86.exe) |
| **Portable Edition (64-bit)** | 64-bit (x64) | `.zip` Archive | [**📦 Download Portable-x64.zip**](https://github.com/Ikdevelopers/PDF-Compressor/releases/latest/download/PDFCompressor-v1.0.0-Portable-x64.zip) |
| **Portable Edition (32-bit)** | 32-bit (x86) | `.zip` Archive | [**📦 Download Portable-x86.zip**](https://github.com/Ikdevelopers/PDF-Compressor/releases/latest/download/PDFCompressor-v1.0.0-Portable-x86.zip) |

*If you are unsure of your Windows version, download the **64-bit Installer**.*

---

## ✨ Why PDF Compressor?

Most online compression websites upload your sensitive personal, medical, or corporate documents to third-party cloud servers. **PDF Compressor is 100% native and offline**. All image downsampling, vector parsing, and stream optimization run exclusively on your local CPU.

- 🔒 **Zero Cloud Telemetry**: Documents never leave your device.
- ⚡ **Dual Processing Power**: Combines **Artifex Ghostscript 10.08.0** (DCT/JPEG re-encoding & bicubic downsampling) with **QPDF 12.4.2** (structural object-stream compression & linearization).
- 🎯 **Preserve Document Structure**: Keeps text, vectors, bookmarks, forms, annotations, and hyperlinks crisp and intact.
- 🛡️ **Space-Inflation Protection**: If a PDF is already heavily optimized, PDF Compressor detects it and retains the original file to avoid bloating.
- 🔐 **Digital Signature Awareness**: Informs users when a PDF contains cryptographic signatures that would be affected by stream rebuilding.

---

## 🎛️ Compression Presets

| Preset | Target Resolution | JPEG Quality | Typical Reduction | Best Use Case |
| :--- | :---: | :---: | :---: | :--- |
| **Extreme** | 72 DPI | 40% | **70% – 95%** | Email attachments, messaging apps, fast mobile sharing |
| **Recommended** *(Default)* | 150 DPI | 75% | **40% – 75%** | Everyday office documents, client invoices, eBooks |
| **High Quality** | 300 DPI | 90% | **20% – 50%** | Portfolios, brochures, print proofs |
| **Low Compression** | 600 DPI | 95% | **10% – 30%** | Archival preservation with lossless object streams |
| **Custom Settings** | Configurable | Configurable | Tailored | Granular slider control over DPI, JPEG quality, font subsetting |

---

## 🖥️ Application Features

### 📄 1. Single PDF Compression
- Fast drag-and-drop workflow.
- Live metadata detection (original size, total page count, signature status).
- Interactive presets and collapsible advanced configuration.
- Real-time progress bar with status feedback.
- Instant post-compression actions: **Open PDF**, **Open Folder**, **Save As...**, and **Copy Path**.

### 📚 2. Batch Compression
- Process hundreds of PDF documents in a single click.
- Add individual files or entire directory trees recursively.
- Configurable parallel worker threads (1 to 8 concurrent processes).
- Per-file status tracking and unified batch summary report.

### 🕒 3. Embedded SQLite History
- Persistent local history storing filenames, original/compressed sizes, and space savings.
- Search documents by name or folder path.
- Sort by date, size saved, or reduction ratio.
- Privacy cleanup: Delete individual records or wipe database in one click.

### 🎨 4. Fluent Windows 11 Design
- Seamless Dark Theme (default) and Light Theme with instant switching.
- Clean rounded card hierarchy and High-DPI support across 100%, 125%, and 150% displays.

---

## 🛠️ Build From Source

### Prerequisites
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (optional, for building installers)

### Commands
```powershell
# Clone repository
git clone https://github.com/Ikdevelopers/PDF-Compressor.git
cd PDF-Compressor

# Build solution
dotnet build PDFCompressor.sln -c Release

# Run automated tests (21 tests)
dotnet test tests/PDFCompressor.Tests/PDFCompressor.Tests.csproj

# Run application
dotnet run --project src/PDFCompressor.App/PDFCompressor.App.csproj
```

---

## 📜 Third-Party Licenses & Compliance

- **Ghostscript 10.08.0**: Licensed under the GNU General Public License v3 (GPLv3).
- **QPDF 12.4.2**: Licensed under the Apache License 2.0.
- **Microsoft.Data.Sqlite**: Licensed under the MIT License.
- Full notices available in [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md).

---

<p align="center">
  Crafted with care by <b>Ikdevelopers</b> • 100% Free & Open Source
</p>

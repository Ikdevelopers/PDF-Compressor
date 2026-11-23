# PDF Compressor – User Guide & Documentation

Welcome to **PDF Compressor**, your offline, native Windows desktop solution for compressing and optimizing PDF documents without losing visual quality.

---

## 📌 Table of Contents
1. [User Interface Overview](#user-interface-overview)
2. [Compression Presets Explained](#compression-presets-explained)
3. [Single PDF Compression](#single-pdf-compression)
4. [Batch PDF Compression](#batch-pdf-compression)
5. [Understanding Compression Results](#understanding-compression-results)
6. [Compression History](#compression-history)
7. [Configuring Settings](#configuring-settings)
8. [Troubleshooting & FAQs](#troubleshooting--faqs)

---

## 🖥️ User Interface Overview

PDF Compressor features a clean, Fluent-inspired Windows 11 interface:
- **Sidebar**:
  - **Home**: Live dashboard featuring total compressed files, total storage saved, and recent activity.
  - **Compress PDF**: Dedicated single-document compression with preview, preset selector, and advanced settings.
  - **Batch Compression**: Multi-file processing with parallel background workers.
  - **Compression History**: Full SQLite-backed activity log with search and sort.
  - **Settings**: Theme selection (Dark / Light), default presets, output directories, and concurrency controls.
  - **About**: Version information, local privacy guarantees, and open-source licenses.
- **Status Bar**: Real-time indication of running CPU architecture (x64 / x86) and PDF engine statuses.

---

## ⚙️ Compression Presets Explained

PDF documents often contain uncompressed high-resolution images, redundant font definitions, and unoptimized object tables. The presets tailor the optimization pipeline for different usage scenarios:

| Preset | Image Resolution | JPEG Quality | Best For | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **Extreme** | 72 DPI | 40% | Email attachments, messaging apps, quick online forms | Maximum size reduction; noticeable downsampling on high-res photos |
| **Recommended** | 150 DPI | 75% | Standard office reports, eBooks, sharing with clients | **Default preset**. Excellent visual balance |
| **High Quality** | 300 DPI | 90% | Print-ready documents, portfolios, visual presentations | Crisp lines and sharp text; moderate size reduction |
| **Low Compression** | 600 DPI | 95% | Archival preservation, CAD/vector blueprints | No image downsampling; flate and object-stream optimization |
| **Custom** | User Configured | User Configured | Specific client specs or custom DPI requirements | Granular slider control over DPI (72-600) and JPEG quality (10-100%) |

---

## 📄 Single PDF Compression

1. Navigate to **Compress PDF** or drag and drop a PDF file into the application window.
2. The file details card displays:
   - File Name
   - Original File Size
   - Number of Pages
   - Digital signature alert (if present)
3. Select your desired preset (e.g., *Recommended*).
4. *(Optional)* Select **Custom Settings** to configure custom DPI, font subsetting, Fast Web View (linearization), or object stream optimization.
5. Click **⚡ Compress PDF Now**.
6. The progress bar displays the active pipeline stage (*Validating* -> *Optimizing Images* -> *Structural Linearization* -> *Done*).
7. Review the **Compression Results** card:
   - Original Size vs. Compressed Size
   - Total Space Saved and Percentage Reduction
   - Total processing time
8. Choose your desired action:
   - **Open PDF**: Opens the compressed file in your default Windows PDF viewer.
   - **Open Folder**: Opens Windows Explorer and selects the new file.
   - **Save As...**: Export the compressed file to any folder.
   - **Copy Path**: Copies the absolute file path to the Windows clipboard.

---

## 📚 Batch PDF Compression

1. Navigate to **Batch Compression**.
2. Add files using any of these methods:
   - Click **+ Add Files** to pick multiple PDFs using the Windows file dialog.
   - Click **+ Add Folder** to scan an entire directory (and subfolders) for PDF files.
   - Drag and drop multiple PDF files or folders directly into the drop zone.
3. Choose the target preset from the toolbar dropdown.
4. Click **▶ Start Batch Compression**.
5. The application compresses files concurrently according to your concurrency setting (default: 2 workers).
6. Progress is tracked individually in the table and globally via the progress bar.
7. Click **Cancel Batch** at any time to safely halt ongoing operations.
8. On completion, click **Open Output Folder** to access all compressed documents.

---

## 📊 Understanding Compression Results

- **Percentage Reduction**: Calculated as `((Original - Compressed) / Original) * 100`.
- **Already Optimized Files**: Some PDFs (such as vector-only documents or documents already compressed with high-efficiency codecs) cannot be made smaller without destructively degrading vector graphics. When this occurs, PDF Compressor alerts you and retains the original file by default, preventing file size inflation.
- **Digital Signatures**: If a document contains a digital signature (`/ByteRange`), the application displays a warning. Compressing a digitally signed PDF rewires stream offsets and will invalidate cryptographic signatures.

---

## 🕒 Compression History

All completed operations are automatically recorded in an embedded SQLite database:
- **Search**: Enter text in the search bar to filter by filename, original folder, or output destination.
- **Sort**: Sort by Date (newest/oldest), Space Saved, Reduction Percentage, or File Name.
- **File Management**: Open the compressed file directly or jump to its location in Windows Explorer.
- **Privacy & Cleanup**: Click **Delete Record** to remove an entry, or **Clear All History** to wipe the local database.

---

## ⚙️ Configuring Settings

- **Appearance**: Toggle between Dark Theme and Light Theme. Changes apply instantly.
- **Default Preset**: Set the preset selected when the application starts.
- **Output Destination**:
  - `Compressed PDFs` subfolder inside the source document's directory (Default).
  - Same directory alongside the source file.
  - Dedicated custom directory on your PC.
- **Batch Concurrency**: Configure simultaneous worker threads (1 to 8) to optimize for your CPU.
- **Automated Actions**: Automatically open Windows Explorer upon completion.
- **Maintenance**: Reset all preferences to factory defaults.

# ZipPeek v1.0

<br>

## ✅ Features

- 📦 Reads ZIP file metadata directly from a remote URL without downloading the full archive.
- 🧠 Extracts file names, compressed size, uncompressed size, compression method, and offset.
- 📂 Displays folder and file structure in a TreeView UI.
- 📏 Shows both compressed and uncompressed sizes next to each file.
- ⚡ Uses HTTP Range Requests to download only the required parts (EOCD + Central Directory).
- 📥 Supports downloading **individual files** from the ZIP archive remotely with high precision.
- 💡 Lightweight and efficient, ideal for cloud-based ZIP inspection and partial extraction tools.

<br>

## ⚠️ Limitations

- ❌ Does not support ZIP64 format (files larger than 4GB or archives with more than 65535 entries).
- ❌ Does not support extracting folders or multiple files at once.
- ❌ Cannot read encrypted or password-protected ZIP archives.
- ⚠️ Will not work if the remote server does not support HTTP Range requests.
- ⚠️ Only standard ZIP format is supported (no split archives, multi-disk, or advanced compression).
- ⚠️ TreeView UI does not yet support search, filter, or sorting (planned in future versions).

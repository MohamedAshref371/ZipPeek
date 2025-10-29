# ZipPeek v1.7

## About This Project

**ZipPeek** is a Windows desktop application built with C# and WinForms, designed to inspect the contents of remote ZIP files **without downloading the entire archive**.

### 🎯 Project Goal

The main goal of ZipPeek is to allow users to:
- Browse the structure of a ZIP archive hosted online.
- View file names, sizes, and paths without downloading the full file.
- Extract specific files or folders on demand.
- Save time and bandwidth when dealing with large remote ZIP files.

It is especially useful in scenarios where:
- You want to preview the contents of a downloadable ZIP before committing to a full download.
- You're working with limited internet speed or storage space.
- You need just one file from a large archive.

### 🛠️ Development Notes

This program was developed with the help of **ChatGPT**, under my full supervision. I guided the process, made design and architecture decisions, and ensured all functionality met the intended use cases.

ChatGPT acted as a coding assistant, helping with logic, implementation ideas, and refining the solution — but the direction, testing, and integration were entirely mine.

<br>

## ✅ Features

- 📦 Reads ZIP file metadata directly from a remote URL without downloading the full archive.
- 🧠 Extracts file names, compressed size, uncompressed size, compression method, and offset.
- 📂 Displays folder and file structure in a TreeView UI.
- 📏 Shows both compressed and uncompressed sizes next to each file.
- ⚡ Uses HTTP Range Requests to download only the required parts (EOCD + Central Directory).
- 📥 Supports downloading **individual files or folders** from the ZIP archive remotely with high precision.
- 🔐 Supports extracting **ZipCrypto-encrypted files** using a user-provided password.
- 🧱 Fully supports **ZIP64 format** (archives larger than 4GB or with more than 65535 entries).
- 🔍 Built-in **search** functionality with up/down navigation.
- ↕️ Supports **sorting** files/folders by name, size, or last modified date (asc/desc).
- 💡 Lightweight and efficient, ideal for cloud-based ZIP inspection and partial extraction tools.

<br>

## ⚠️ Limitations

- ⚠️ Only **ZipCrypto** encryption is supported. Archives using **AES encryption** are not currently supported.
- ⚠️ Will not work if the remote server does not support HTTP Range requests.
- ⚠️ Only standard ZIP format is supported (no split archives, multi-disk, or advanced compression).

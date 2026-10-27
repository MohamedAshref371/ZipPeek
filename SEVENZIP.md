# Remote 7z archives

Select **7z** beside the URL, optionally enter the password, and click **Read**.
The URL is passed to the selected archive handler; its extension is not used to
guess the format. Invalid archives are still rejected by the handler.

## Requirements

- The server must support byte ranges and report the archive length in
  `Content-Range`. Encoded HTTP bodies are rejected.
- Install [7-Zip](https://www.7-zip.org/), or put its `7z.dll` in a `7zip.org`
  subdirectory beside `ZipPeek.exe` (placing the DLL directly beside the EXE also
  works). The DLL must match the process architecture. AnyCPU builds now run as
  64-bit on 64-bit Windows, and as 32-bit on 32-bit Windows.
- Native 7-Zip is an external dependency; no native binary is bundled by this
  change. ZIP reading continues to work without it. See the existing
  [7-Zip license notice](licenses/7Zip-LICENSE.txt) and the
  [official license](https://www.7-zip.org/license.txt) when distributing the DLL.

## Selection and download behavior

- Browsing requests the signature and header data needed by the native engine,
  including compressed or encrypted headers. Encrypted headers require a password
  before browsing. Encrypted file data requires a password before extraction.
- Selecting a file includes every file sharing its compression block. Selecting
  a folder first applies the subfolder setting, then expands the selection to the
  required blocks. Expansion can include files outside that folder.
- The confirmation lists the expanded file count and required compressed data.
  If the selection spans every data block, a prominent warning states that the
  **entire archive** will be downloaded and all files included for extraction.
  This includes multi-file archives with one solid block. Declining is the default.
- After confirmation, an entire-archive operation caches all archive bytes;
  partial operations let the engine request the required ranges. Already cached
  ranges are reused in both cases.
- Existing-file settings apply to all expanded files. If every output is skipped,
  there is no additional download. With the skip-failed setting, per-file decoding
  failures are skipped when the engine can continue, and a summary reports partial
  success. With the retry setting, an error offers to retry the selection using
  cached data. Network and filesystem failures stop the current attempt.
- Files are written to temporary siblings and committed only after successful
  extraction and size verification. Existing destination files remain intact when
  extraction fails. Archive paths escaping the output directory, conflicting
  names, Windows device names, and existing junctions/symlinks are rejected.
- A loaded archive owns its temporary disk cache. Canceling preserves completed
  cached ranges for another attempt; switching format, rereading, or closing
  discards the cache. This is not persistent resumption across application runs.
- Archive size, ETag and Last-Modified values are checked when available to avoid
  mixing bytes from different remote revisions. A detected change requires rereading.

The tree marks shared compressed sizes as **shared block**. Sorting uses block
sizes, and folder-size calculations count each required block once. Block sizes
exclude archive header overhead; progress shows actual transferred bytes.

Split/multi-volume archives and self-extracting wrappers are outside this support.
The selected source must be a standalone 7z archive.

## Implementation

`HttpRangeStream` implements seekable, bounded-memory access over HTTP. Requested
ranges are appended to a temporary file with an in-memory segment index; the
remote archive is never represented by a zero-filled, full-size local file.
`SevenZipNative` defines the official SDK interfaces and loads the installed
engine. `RemoteSevenZipArchive` owns metadata, download planning and extraction;
`Form1.SevenZip.cs` contains the WinForms workflow.

The integration checks generate real 7z fixtures and use the installed engine
with a deterministic HTTP range handler. See [tests/README.md](tests/README.md).

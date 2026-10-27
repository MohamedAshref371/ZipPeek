# Code maintenance

Remote 7z support is documented in [SEVENZIP.md](SEVENZIP.md). Its HTTP stream,
native interop, extraction and UI workflow are separate files. ZIP extraction
continues to use the existing implementation.

The first refactoring isolates HTTP range transfers in `RangeDownloader`.
`DownloadManager` keeps the existing application API and owns cancellation sources;
`RemoteZipReader` and `RemoteZipExtractor` delegate range downloads to it.
Request headers, timing, and buffers belong to each transfer. HTTP responses are
checked before their bytes are consumed, and incomplete transfers fail explicitly.
The HTTP client is injectable into the transfer implementation for offline checks.
See [tests/README.md](tests/README.md) for commands and coverage limits.

Further work should stay in independently verifiable changes:

- Move download orchestration out of `Form1`. Return a typed result for success,
  failure, and cancellation instead of checking status-label text to decide retries.
- Share local ZIP header parsing between the memory and disk extraction paths.
  Bound stored-file copies to the entry size, so trailing ZIP records cannot be
  written into the extracted file.
- Resolve archive entry paths under explicit download and temporary directories
  before creating files, with checks that paths stay inside those directories.
- Validate central-directory and extra-field lengths before reading binary values;
  cover ZIP64 markers, long filenames, malformed records, and legacy encodings.
- Replace integer folder options with enums and reduce static UI state in
  `FolderSetting` and `TreeViewHelper`.
- Check external 7-Zip availability and exit status, and dispose its process.
- Reconcile dependency versions and their assembly references in a separate
  dependency change.

The current change retains both extraction strategies and the existing UI. Full
archive and UI regression coverage is still needed before larger restructuring.

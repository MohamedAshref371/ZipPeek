# Download regression checks

Build with Visual Studio MSBuild and the .NET Framework 4.8 targeting pack:

```powershell
msbuild tests/ZipPeek.Tests.csproj /p:Configuration=Debug
./tests/bin/Debug/ZipPeek.Tests.exe
```

The executable returns a nonzero exit code on failure. It uses an in-memory HTTP
handler, needs no NuGet packages, and does not contact external servers. Production
download sources are linked into the test project so it runs the same implementation.

Checks cover range validation, HTTP range support, incorrect Content-Range,
truncated responses, clipping at EOF, final progress, file resumption, already
complete and oversized temporary files, cancellation, independent concurrent
requests, and empty central directories.

These checks do not cover the full ZIP parser or encrypted ZIP extraction.

## 7z integration checks

Install 7-Zip with the same architecture as the test process, then run:

```powershell
./tests/bin/Debug/ZipPeek.Tests.exe --sevenzip
```

This also generates real LZMA2 archives using `7z.exe` and reads/extracts them
through `7z.dll`. Fixtures cover non-solid archives, multiple solid blocks,
one solid block, compressed headers, encrypted headers and encrypted data. A
deterministic HTTP handler measures transferred ranges and tests cache reuse,
whole-archive planning, empty files, replacement, cancellation/resumption,
changed remote data, corrupt data and path confinement. Generated fixtures and
outputs live under the test executable's directory and are deleted afterward.

The suite exercises the native engine and production download/extraction code;
it does not automate WinForms dialogs or contact external servers.

After a Debug build, run the UI smoke checks in an STA PowerShell process:

```powershell
powershell.exe -NoProfile -STA -File tests/SevenZip.UiSmoke.ps1
```

These use an off-screen form and sample entries to check selection, lazy folders,
subfolder settings, shared-block size totals and busy state. They render a preview
to `tests/obj/sevenzip-ui.png` without changing saved application settings.

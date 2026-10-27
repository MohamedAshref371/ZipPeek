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

These checks do not cover the WinForms interface, encrypted extraction, external
7-Zip execution, or the full ZIP parser.

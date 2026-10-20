# Building and running NyaImageTool

This document describes how to build, test and run `nyaimagetool` on Windows and
Ubuntu. The CLI targets **.NET 10** and loads its plugins from a `plugins` folder
next to the executable.

## 1. Requirements

- **.NET SDK 10.0.x** (the project targets `net10.0`; .NET 3.1/6/8/9 SDKs are not enough).
- **A local NuGet feed** with the packages below. They are not all available on nuget.org:
  - `NyaFs` **1.0.53** (nuget.org currently has only 1.0.8),
  - `NyaXZ` **1.0.0** (not published on nuget.org),
  - `NyaLZO` **1.0.2** (also available on nuget.org).
- The other dependencies (`NyaIO`, `SharpCompress`, `SharpZipLib`, `ZstdSharp.Port`,
  `CrcSharp`, `FT.LZ4`, `LZMA-SDK`, xunit, ...) restore from nuget.org.
- Git (optional; needed only if you build `NyaFs`/`NyaXZ` from source).

The repository has no `NuGet.config`; the feed is configured per user with
`dotnet nuget add source`.

## 2. Prepare the local NuGet feed

### Windows

If the feed already exists in this workspace, use `G:\Projects\local.nuget`.
Otherwise create it and pack `NyaFs` (requires `NyaXZ.1.0.0.nupkg` in the feed):

```powershell
New-Item -ItemType Directory -Force G:\Projects\local.nuget

# Pack NyaXZ first if the feed does not contain it yet
dotnet pack "G:\Projects\Асик конфиг\nyaxz\src\NyaXZ\NyaXZ.csproj" -c Release -o G:\Projects\local.nuget

# Pack NyaFs
dotnet pack "G:\Projects\Асик конфиг\nyafs\src\NyaFs\NyaFs.csproj" -c Release -o G:\Projects\local.nuget

# Register the feed once
dotnet nuget add source G:\Projects\local.nuget --name nyafs-local
dotnet nuget list source
```

### Ubuntu / Linux

Copy the `.nupkg` files from an existing feed (or build them), then register the folder:

```bash
mkdir -p ~/nyafs-nuget
cp /path/to/NyaFs.1.0.53.nupkg   ~/nyafs-nuget/
cp /path/to/NyaXZ.1.0.0.nupkg    ~/nyafs-nuget/
cp /path/to/NyaLZO.1.0.2.nupkg   ~/nyafs-nuget/

dotnet nuget add source ~/nyafs-nuget --name nyafs-local
dotnet nuget list source
```

Building the packages from source on Linux:

```bash
dotnet pack ~/src/nyaxz/src/NyaXZ/NyaXZ.csproj -c Release -o ~/nyafs-nuget
dotnet pack ~/src/nyafs/src/NyaFs/NyaFs.csproj -c Release -o ~/nyafs-nuget
```

## 3. Install the .NET SDK

### Windows

```powershell
winget install --id Microsoft.DotNet.SDK.10 -e
dotnet --version   # must print 10.0.x
```

Alternative: download the SDK from <https://dotnet.microsoft.com/download/dotnet/10.0>.

### Ubuntu

Microsoft package feed (example for Ubuntu 24.04):

```bash
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
sudo dpkg -i /tmp/packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
dotnet --version   # must print 10.0.x
```

Or install without root into `~/.dotnet`:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 10.0
export PATH="$HOME/.dotnet:$PATH"
dotnet --version
```

If .NET reports missing ICU/SSL libraries on a minimal Ubuntu image, install
`libicu-dev` and the usual runtime dependencies (`libssl3`, `zlib1g`).

## 4. Build

Always build the **solution**, not only the `NyaImageTool` project: plugin projects
are built separately and their PostBuild step copies the DLLs into
`NyaImageTool/bin/<Configuration>/net10.0/plugins`.

### Windows

```powershell
cd "G:\Projects\Асик конфиг\nyaimagetool"
dotnet restore NyaImageTool.sln
dotnet build NyaImageTool.sln -c Release -maxcpucount:1
```

### Ubuntu

```bash
cd ~/src/nyaimagetool
dotnet restore NyaImageTool.sln
dotnet build NyaImageTool.sln -c Release -maxcpucount:1
```

`-maxcpucount:1` is recommended on low-memory machines and avoids parallel
plugin-copy races.

For the internal plugin set (adds Fanvil and IncmEncrypt) use
`NyaImageToolInternal.sln` instead of `NyaImageTool.sln`.

## 5. Run

### Windows

```powershell
dotnet NyaImageTool\bin\Release\net10.0\NyaImageTool.dll
dotnet NyaImageTool\bin\Release\net10.0\NyaImageTool.dll script.nyas
```

You can also start `NyaImageTool.exe` from the same folder. Without arguments the
interactive shell starts.

### Ubuntu

```bash
dotnet NyaImageTool/bin/Release/net10.0/NyaImageTool.dll
dotnet NyaImageTool/bin/Release/net10.0/NyaImageTool.dll script.nyas
```

Or directly from source:

```bash
dotnet run --project NyaImageTool -c Release -- script.nyas
```

Plugins are searched in `AppContext.BaseDirectory/plugins` (next to the DLL) and
in `./plugins`. Running the DLL by its path therefore works from any directory.

### Useful commands

```
inspect <file>
load <file>
load.sub <file> <subpath>
extract <file> <subpath> <out>
store <out> <type> <format>
export <dir>
```

See [`loading_examples.md`](loading_examples.md) for details.

### Path gotcha

Script arguments are split on whitespace. If an image path contains spaces or
Cyrillic characters, copy the image to a simple path first (for example
`F:\TEMP` on Windows or `/tmp` on Linux) and pass that path to `load`.

## 6. Tests

```powershell
dotnet test NyaImageTool.sln -c Release --no-build
```

The regression tests use the image corpus from `NYAIMAGETOOL_IMAGES`
(default `F:\TEMP\Images`) and `NyaFsTests/Regression/golden.json`. If the corpus
is absent, the corpus-based tests skip; on Linux set:

```bash
export NYAIMAGETOOL_IMAGES=/path/to/Images
```

The expected result for the public repository is **141/141**. The golden file is
ignored by Git; update it only intentionally and keep a backup before
regenerating (`NYAIMAGETOOL_REGEN=1`).

## 7. Publish / distribute

Framework-dependent publish (target machine needs the .NET 10 runtime):

```powershell
dotnet publish NyaImageTool\NyaImageTool.csproj -c Release -r win-x64 --self-contained false -o dist\win-x64
```

```bash
dotnet publish NyaImageTool/NyaImageTool.csproj -c Release -r linux-x64 --self-contained false -o dist/linux-x64
```

Self-contained (no runtime install required on the target):

```bash
dotnet publish NyaImageTool/NyaImageTool.csproj -c Release -r linux-x64 --self-contained true -o dist/linux-x64
```

`dotnet publish` of the app project does not build the plugin projects. After
publishing, copy the plugin folder built by the solution:

```powershell
Copy-Item -Recurse NyaImageTool\bin\Release\net10.0\plugins dist\win-x64\plugins
```

```bash
cp -r NyaImageTool/bin/Release/net10.0/plugins dist/linux-x64/plugins
```

The simplest distribution is the complete `NyaImageTool/bin/Release/net10.0`
folder: it already contains the app, dependencies and `plugins`.

## 8. Plugins

Public `NyaImageTool.sln` builds Allwinner, Bmu, Encrypt, Files, Innosilicon,
Linux, McuFs, Remote, Ftp, Rockchip, Sftp and Tftp plugins, plus the shared
`NyaFsRemoteCore` library. `NyaImageToolInternal.sln`
adds Fanvil and IncmEncrypt. The private `NyaFsAmlogicEncrypted` plugin is not in
either solution; build it directly (its PostBuild copies it to the app plugins
folder):

```bash
dotnet build Plugins/NyaFsAmlogicEncrypted/NyaFsAmlogicEncrypted.csproj -c Release
```

Plugin projects can be built standalone: the copy target is relative to the
project file (`$(MSBuildThisFileDirectory)../../NyaImageTool/bin/...`), so it does
not depend on `$(SolutionDir)`.

Plugin-specific dependencies are copied by the plugin PostBuild targets
(for example `Tftp.Net.dll` for Tftp, and `Renci.SshNet.dll` +
`BouncyCastle.Cryptography.dll` + `Microsoft.Extensions.*` for Remote). If you add
a new dependency to a plugin, add it to that project's `<Copy SourceFiles="...">`
list so it lands in the `plugins` folder.

`NyaFsRemote` is optional: omit it (and its SSH.NET/BouncyCastle DLLs) from
`plugins` if remote SCP/SFTP/SSH transfers are not needed. `NyaFsFtp` (FTP) and
HTTP downloads use the built-in .NET implementations and add no DLLs.

## 9. Updating NyaFs

When `NyaFs` changes:

1. In the `nyafs` repository bump `<Version>` in `src/NyaFs/NyaFs.csproj`.
2. Pack it into the local feed:
   `dotnet pack src/NyaFs/NyaFs.csproj -c Release -o <feed>`.
3. In every `*.csproj` of `nyaimagetool` update
   `Include="NyaFs" Version="..."` to the new version.
4. `dotnet restore` + `dotnet build NyaImageTool.sln` and run the tests.

## 10. Troubleshooting

- **`NU1101`/`NU1102` for NyaFs or NyaXZ** — the local feed is not configured or
  does not contain the required version. Check `dotnet nuget list source` and the
  folder contents.
- **`NETSDK1045` / unsupported `net10.0`** — install the .NET 10 SDK, not just an
  older SDK/runtime.
- **Plugins are not loaded** — build the solution (not only the app project) and
  check that `plugins` is next to `NyaImageTool.dll`. If running the app from a
  published folder, copy `plugins` there.
- **`dotnet` not found on Ubuntu** — add `~/.dotnet` to `PATH` or install the SDK
  system-wide.
- **Old checkout with `$(SolutionDir)` in plugin copies** — update to the current
  plugin targets; building a plugin project standalone used to fail with
  `*Undefined*`.
- **GitHub Actions workflow** — `.github/workflows/dotnet.yml` is outdated
  (.NET 3.1/6.0) and does not configure the local feed; do not use it as a local
  build recipe.

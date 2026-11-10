# Plugin filesystem options

External filesystem plugins can expose the same declarative options as built-in
filesystems.

## Declare options

```csharp
public override FilesystemOption[] Options => new[]
{
    new FilesystemOption("compression", FilesystemOptionType.Enum, "none",
        "Data compression", new[] { "none", "zlib", "lzo" }),
    new FilesystemOption("block-size", FilesystemOptionType.Size, "4096",
        "Logical block size")
};
```

Supported types: `String`, `Bool`, `Integer`, `Size`, `Enum`.

`Size` accepts decimal/hex values and `K`, `M`, `G` suffixes.

## Read options in the writer

```csharp
public override Writer GetFilesystemWriter(LinuxFilesystem Fs)
{
    string Compression = Fs.GetFilesystemOption(Format, "compression", "none");
    string BlockSize = Fs.GetFilesystemOption(Format, "block-size", "4096");
    // build the writer with these values
}
```

## Help

```text
help
help <command>
help <filesystem-format>
```

`help` lists commands and all filesystem formats. `help <format>` shows the
options declared by that plugin; `help <command>` shows its argument forms.

## Containers

Containers implement `CompositeImagePlugin`, not `FilesystemPlugin`. A container
describes its child elements through `GetImages`; their kinds are detected
recursively. CIMG is a container (usually carrying a gzip payload), not a
filesystem.

Container options use the same descriptors and are stored in the image blob:

```text
set container <format>.<option> <value>
```

Container header metadata is read on load and pre-fills matching options (for
CIMG: `label`, `offset`, `part-size`). A container can also be written from all
loaded children:

```text
store out.cimg all cimg
```

`help <container-format>` shows the declared options.

## Kernel and device-tree plugins

`KernelPlugin` and `DevtreePlugin` expose the same metadata and option API:

```csharp
public override string[] Aliases => new[] { "linux-zimage" };
public override FilesystemOption[] Options => new[]
{
    new FilesystemOption("compression", FilesystemOptionType.Enum, "none",
        "Payload compression", new[] { "none", "gzip" })
};
```

The values are stored in `LinuxKernel.GetKernelOption(...)` and
`DeviceTree.GetDevtreeOption(...)`. CLI field names follow the same pattern:

```text
set kernel <format>.<option> <value>
set devtree <format>.<option> <value>
```

## Metadata

A plugin can also expose a display name, CLI aliases and common extensions:

```csharp
public override string DisplayName => "My filesystem";
public override string[] Aliases => new[] { "myfs", "my-fs" };
public override string[] Extensions => new[] { "myfs" };
```

The canonical `Format` remains the value written into images and logs.
`LinuxFilesystem.FilesystemFormat` keeps that canonical name for loaded
external filesystems, including FIT/export flows.

## CLI

```text
set ramfs <format>.<option> <value>
```

For the example above:

```text
set ramfs myfs.compression lzo
set ramfs myfs.block-size 64K
```

Values are validated by the plugin descriptor and stored in the current
`LinuxFilesystem`. `Set` no longer has a hardcoded list of filesystem options.

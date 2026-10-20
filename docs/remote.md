# Remote file transfer

Remote transfers are split into two optional plugins:

- **`NyaFsRemote`** — `ssh`, `scp`, `sftp`;
- **`NyaFsFtp`** — `ftp` (built-in `FtpWebRequest`, no SSH dependency).

Both use the shared `NyaFsRemoteCore` transfer layer (recursive get/put,
metadata, symlinks). HTTP/HTTPS downloads are handled by the `download` command
in `NyaFsFiles` (no external dependencies).

## Protocols

- **ssh** — an SSH exec channel with `cat` (works when the device has neither
  SFTP nor SCP); metadata via `stat`/`ls`/`readlink`, contents via stdin/stdout.
- **scp** — SSH metadata + SCP for file contents.
- **sftp** — SFTP for metadata and contents.
- **ftp** — passive FTP; `NyaFsFtp` plugin.

## Commands

```
remoteget <protocol> <host[:port]> <login> <pass> <fs-path> <remote-path>
remoteput <protocol> <host[:port]> <login> <pass> <fs-path> <remote-path>
```

`<protocol>` is `ssh`, `scp` or `sftp`.

```
ftpget <host[:port]> <login> <pass> <fs-path> <remote-path>
ftpput <host[:port]> <login> <pass> <fs-path> <remote-path>
```

- `...get` copies a remote file or directory into the loaded filesystem.
- `...put` copies a filesystem file or directory to the remote device.
- `<host[:port]>`: port defaults to 22 for ssh/scp/sftp and 21 for ftp.
  IPv6 is accepted as `[::1]:2222`.
- Directory transfers are recursive.
- SSH/SCP preserve symlinks; SFTP symlink targets are not read yet, such
  entries are skipped with a warning. Special files are skipped on upload.
- Existing files are overwritten; parent directories are created.

The old `copysshfrom <host[:port]> <login> <pass> <fs-path> <remote-path>`
command remains as an alias for `remoteget ssh ...`.

## Examples

```text
# Pull a config from a device into the loaded image
remoteget ssh 192.168.1.10:22 root secret /etc/config /etc/config

# Push a file to a device that has only an SSH server (no sftp/scp)
remoteput ssh 192.168.1.10 root secret /tmp/firmware.bin /tmp/firmware.bin

# Copy a whole directory over SFTP
remoteget sftp 192.168.1.10 root secret /usr/share/flags /usr/share/flags

# FTP (passive mode, NyaFsFtp plugin)
ftpget 192.168.1.20 user pass /pub/rootfs.bin /rootfs.bin
ftpput 192.168.1.20 user pass /tmp/log.txt /upload/log.txt

# HTTP/HTTPS download (NyaFsFiles plugin)
download F:\TEMP\rootfs.bin https://example.org/rootfs.bin
```

Credentials are passed as script arguments, so keep scripts containing
passwords out of version control.

## Device requirements for `ssh`

The remote shell must provide the usual BusyBox/GNU utilities:
`stat`, `ls`, `cat`, `mkdir`, `readlink`, `test`. `ssh` uses password
authentication; key/agent authentication is not exposed in the command yet.

## Plugin dependencies

- `NyaFsRemote` pulls SSH.NET (and its BouncyCastle dependency).
- `NyaFsFtp` uses the built-in .NET FTP implementation and adds no DLLs.
- `NyaFsRemoteCore` contains the shared transfer logic and has no external
  dependencies.

If remote access is not needed, omit these plugin DLLs (and the SSH.NET/
BouncyCastle DLLs) from the `plugins` folder; the base application continues to
work. To support FTP only, ship `NyaFsFtp.dll` + `NyaFsRemoteCore.dll` without
`NyaFsRemote`/SSH.NET.

## Verification

`NyaFsTests/RemoteTransferTests` covers the protocol-independent transfer logic
(recursive get/put, metadata, symlinks, host/port parsing, shell quoting).
The FTP transport was additionally verified end-to-end against a local
`pyftpdlib` server (file, directory, upload). Live SSH/SCP/SFTP need a reachable
device or server and are not part of the unit suite.

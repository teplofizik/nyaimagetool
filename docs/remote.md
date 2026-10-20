# Remote file transfer

The remote transfer layer lives in the **NyaFs core** (`NyaFs.Remote`): recursive
get/put, metadata, symlinks, host/port parsing. Protocol support is provided by
optional plugins:

- **`NyaFsRemote`** — `ssh`, `scp`, `sftp`;
- **`NyaFsFtp`** — `ftp` (built-in `FtpWebRequest`, no SSH dependency).

HTTP/HTTPS downloads are handled by the `download` command in `NyaFsFiles`
(no external dependencies).

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

`<protocol>` is `ssh`, `scp`, `sftp` or `ftp`. If no loaded plugin provides the
requested protocol, the command fails with
`Protocol '<name>' is not available: no remote plugin is loaded`.

The `<login> <pass>` pair can be replaced by an auth profile:
`@profile -` (the second argument is ignored). Profiles keep passwords and key
paths out of `.nyas` scripts; see [Auth profiles](#auth-profiles).

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
command remains as an alias for `remoteget ssh ...`. Likewise `ftpget`/`ftpput`
remain as aliases for `remoteget ftp`/`remoteput ftp`.

## Examples

```text
# Pull a config from a device into the loaded image
remoteget ssh 192.168.1.10:22 root secret /etc/config /etc/config

# Push a file to a device that has only an SSH server (no sftp/scp)
remoteput ssh 192.168.1.10 root secret /tmp/firmware.bin /tmp/firmware.bin

# Copy a whole directory over SFTP
remoteget sftp 192.168.1.10 root secret /usr/share/flags /usr/share/flags

# FTP (passive mode, NyaFsFtp plugin)
remoteget ftp 192.168.1.20 user pass /pub/rootfs.bin /rootfs.bin
remoteput ftp 192.168.1.20 user pass /tmp/log.txt /upload/log.txt

# HTTP/HTTPS download (NyaFsFiles plugin)
download F:\TEMP\rootfs.bin https://example.org/rootfs.bin
```

## Auth profiles

Credentials are stored outside scripts in a small profile file:

```
remoteauth
remoteauth list
remoteauth path
remoteauth show <name>
remoteauth add <name> <login> <password>
remoteauth addkey <name> <login> <key-file> [passphrase]
remoteauth remove <name>
```

- `remoteauth` with no arguments is the same as `remoteauth list`.
- `-` as `<passphrase>` means an empty passphrase; on inline commands `-` passes
  an empty password to the server.
- `addkey` supports OpenSSH and PEM private keys; an encrypted key needs its
  passphrase.
- An SSH profile may contain a password, a key, or both. FTP supports password
  authentication only.
- `show` and `list` never print passwords or passphrases.

Storage location:

- Windows: `%APPDATA%\NyaFs\auth.txt`
- Linux/Unix: `$XDG_CONFIG_HOME/nyafs/auth.txt` or `~/.config/nyafs/auth.txt`
- Override with `NYAFS_AUTH_FILE`.

The file stores values as base64 and is created with mode `0600` on Unix. It is
still a secret file: do not commit it. Windows DPAPI encryption is not applied
yet.

Examples:

```text
# Save a password profile and use it
remoteauth add device root secret
remoteget ssh 192.168.1.10 @device - /etc/config /etc/config

# Save a private-key profile
remoteauth addkey device root C:\keys\id_ed25519
remoteput sftp 192.168.1.10 @device - /tmp/firmware.bin /tmp/firmware.bin
```

## Device requirements for `ssh`

The remote shell must provide the usual BusyBox/GNU utilities:
`stat`, `ls`, `cat`, `mkdir`, `readlink`, `test`. `ssh` supports password and
private-key authentication (with passphrase); ssh-agent is not supported yet.

## Plugin dependencies

- The core transfer layer is part of `NyaFs.dll`; no separate core DLL is
  shipped.
- `NyaFsRemote` pulls SSH.NET (and its BouncyCastle dependency).
- `NyaFsFtp` uses the built-in .NET FTP implementation and adds no DLLs.

If remote access is not needed, omit these plugin DLLs (and the SSH.NET/
BouncyCastle DLLs) from the `plugins` folder; the base application continues to
work. To support FTP only, ship `NyaFsFtp.dll` without
`NyaFsRemote`/SSH.NET.

## Verification

`NyaFsTests/RemoteTransferTests` covers the protocol-independent transfer logic
(recursive get/put, metadata, symlinks, host/port parsing);
`NyaFsTests/RemoteAuthTests` covers profile storage and `@profile` resolution.
The FTP transport was additionally verified end-to-end against a local
`pyftpdlib` server (file, directory, upload). Live SSH/SCP/SFTP need a reachable
device or server and are not part of the unit suite.

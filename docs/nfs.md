# NFS server

`NyaFsNfs` exports the loaded filesystem over **NFSv3** so it can be mounted as
a normal directory and edited with a file manager or shell. This is the
server-side counterpart of `remoteget`/`remoteput`.

## Start / stop

```
service nfs start
service nfs stop
service nfs info
```

Configuration (before `start`):

```
service nfs set port 2049       # NFS, default 2049
service nfs set mountport 20048 # MOUNT, default 20048
service nfs set portmap 111     # portmapper; 0 disables it
```

The plugin listens on both TCP and UDP. `portmap` may fail on Windows without
administrator rights or when port 111 is already used; this is only a warning,
and the NFS/MOUNT ports still work.

## Mounting

Linux, without rpcbind (explicit ports):

```bash
sudo mkdir -p /mnt/nya
sudo mount -t nfs -o nfsvers=3,proto=tcp,port=2049,mountport=20048,nolock \
  192.168.1.10:/ /mnt/nya
```

Linux, with a working portmapper:

```bash
sudo mount -t nfs -o nfsvers=3,proto=tcp,nolock 192.168.1.10:/ /mnt/nya
```

Windows NFS client (Services for NFS) uses the export path `/`:

```text
mount -o anon \\192.168.1.10\
```

Use `umount /mnt/nya` (Linux) or `umount \\192.168.1.10\` (Windows) when done.

## Editing model

- The server exposes the current in-memory filesystem; changes are visible
  immediately to other clients.
- Changes are **not** written to the image automatically. Use `store` after
  editing, exactly as with SFTP.
- Writes are serialized inside the plugin, but NFS file locking is not
  implemented. Mount with `nolock`; use one writer at a time.
- The export has no authentication or access control. Run it only on a trusted
  network; any client that can reach the port has full access to the mounted
  filesystem.

## What is implemented

- NFSv3: GETATTR, SETATTR, LOOKUP, ACCESS, READLINK, READ, WRITE, CREATE,
  MKDIR, SYMLINK, REMOVE, RMDIR, RENAME, READDIR, READDIRPLUS, FSSTAT, FSINFO,
  PATHCONF, COMMIT.
- MOUNT v3 and a minimal portmapper (GETPORT/DUMP) over TCP and UDP.
- Symlinks and standard permissions/uid/gid are preserved in the in-memory
  model.
- Not implemented: NFSv4, file locking, hard links (`LINK`), device nodes
  (`MKNOD`), ACLs and quotas. `LINK`/`MKNOD` return `NFS3ERR_NOTSUPP`.

## Verification

`NyaFsTests/NfsProbeTests` starts the plugin on high ports and exercises the
RPC/MOUNT/NFSv3 paths over TCP and UDP (mount, lookup, readdir/readdirplus,
read, create, write, rename, remove, setattr, fsstat/fsinfo/pathconf/commit,
portmapper GETPORT). A real `mount -t nfs` test needs a separate Linux host or
WSL instance.

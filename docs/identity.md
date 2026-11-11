# Users and groups inside an image

Linux filesystems store only numeric `uid`/`gid` on inodes. Names are stored in
the image itself:

- `/etc/passwd` — `name:password:uid:gid:gecos:home:shell`
- `/etc/group` — `name:password:gid:members`

`LinuxFilesystem.Identities` is a lazy `FsIdentityDatabase` view over those two
files. It is a cache/editor, not additional filesystem metadata. Numeric ids
remain canonical; when a name is unknown, commands and `ls` fall back to the
numeric id.

## Commands

```text
user list
user add <name> <uid> <gid> [home] [shell]
user del <name>

group list
group add <name> <gid>
group del <name>
group adduser <user> <group>
group deluser <user> <group>

chown <path> <user>[:<group>]
chown <path> <user> <group>
chgrp <path> <group>
```

Names in `chown`/`chgrp` are resolved through the loaded filesystem:
`chown etc/init.d/S99app root:root`. Numeric ids still work:
`chown etc 0 0`.

`user add`, `user del` and the group commands update `/etc/passwd`/`/etc/group`
in the in-memory filesystem and mark them modified, so the next `store`/`export`
writes the updated files. Existing lines and unknown fields are preserved;
`/etc/shadow` is not touched.

## Behaviour

- If `/etc/passwd` or `/etc/group` is absent, numeric editing still works;
  name lookup fails with a clear error.
- `user add` creates `/etc/passwd` (and group commands create `/etc/group`) if
  the parent directory exists.
- Duplicate names/ids are rejected; malformed lines are preserved and reported
  as warnings.
- Removing a user also removes that name from group member lists.
- `/etc/passwd` and `/etc/group` are ordinary image files: after `store` they
  are part of the produced filesystem exactly like any other file.

## NyaFsLinux plugin

The optional `NyaFsLinux` plugin reuses `Fs.Identities` for `/etc/passwd` and
`/etc/group` and adds Linux account administration plus `/etc/shadow` handling:

```text
lsusr
lshashes
adduser <name> <uid> [gid] [home] [shell]
useradd <name> <uid> <gid> [home] [shell]
usermod <name> <home> <shell>
usermod <name> <uid> <gid> <home> <shell>
userdel <name>
groupadd <name> <gid>
groupmod <name> <gid>
groupdel <name>
mkpasswd <user> <password>
passwd <user> [password]
findpasswd <user> <listfile>
```

`adduser` creates a matching primary group when GID is omitted. `useradd` adds a
locked shadow entry (`!`) when `/etc/shadow` exists (and creates the empty file
when the parent directory exists), and creates the home directory if its parent
directory exists. `groupmod` updates the GID and the primary GID of users that
referenced the old value. `userdel` removes the shadow
entry. Password commands still operate only on `/etc/shadow`.

`ls` uses the database for display, with numeric fallback:

```text
-rw-r--r--     root     root          940 inittab
```

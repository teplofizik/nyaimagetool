using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.Filesystem.Universal.Types;
using NyaFsNfs.Protocol;
using System;
using System.Collections.Generic;

namespace NyaFsNfs
{
    internal sealed class NfsV3
    {
        private const uint Nfs3Ok = 0;
        private const uint Nfs3ErrNoEnt = 2;
        private const uint Nfs3ErrIO = 5;
        private const uint Nfs3ErrAcces = 13;
        private const uint Nfs3ErrExist = 17;
        private const uint Nfs3ErrNotDir = 20;
        private const uint Nfs3ErrIsDir = 21;
        private const uint Nfs3ErrInval = 22;
        private const uint Nfs3ErrNameTooLong = 63;
        private const uint Nfs3ErrNotEmpty = 66;
        private const uint Nfs3ErrStale = 70;
        private const uint Nfs3ErrBadHandle = 10001;
        private const uint Nfs3ErrNotSupp = 10004;

        private const uint Nf3Reg = 1;
        private const uint Nf3Dir = 2;
        private const uint Nf3Blk = 3;
        private const uint Nf3Chr = 4;
        private const uint Nf3Lnk = 5;
        private const uint Nf3Sock = 6;
        private const uint Nf3Fifo = 7;

        private const uint TimeDontChange = 0;
        private const uint TimeServer = 1;
        private const uint TimeClient = 2;

        private const uint FsId = 0x4E594653;

        private readonly NfsFs Fs;

        public NfsV3(NfsFs Fs)
        {
            this.Fs = Fs;
        }

        public byte[] Dispatch(RpcCall Call)
        {
            if (Call.Version != 3)
                return Rpc.ProgramMismatch(Call.Xid, 3, 3);

            try
            {
                var Body = new XdrWriter();

                lock (Fs.SyncRoot)
                switch (Call.Procedure)
                {
                    case 0: break;
                    case 1: GetAttr(Call, Body); break;
                    case 2: SetAttr(Call, Body); break;
                    case 3: Lookup(Call, Body); break;
                    case 4: Access(Call, Body); break;
                    case 5: ReadLink(Call, Body); break;
                    case 6: Read(Call, Body); break;
                    case 7: Write(Call, Body); break;
                    case 8: Create(Call, Body); break;
                    case 9: MkDir(Call, Body); break;
                    case 10: SymLink(Call, Body); break;
                    case 11: MkNode(Call, Body); break;
                    case 12: Remove(Call, Body, false); break;
                    case 13: Remove(Call, Body, true); break;
                    case 14: Rename(Call, Body); break;
                    case 15: Link(Call, Body); break;
                    case 16: ReadDir(Call, Body, false); break;
                    case 17: ReadDir(Call, Body, true); break;
                    case 18: FsStat(Call, Body); break;
                    case 19: FsInfo(Call, Body); break;
                    case 20: PathConf(Call, Body); break;
                    case 21: Commit(Call, Body); break;
                    default: return Rpc.Error(Call.Xid, Rpc.AcceptProcedureUnavailable);
                }

                return Rpc.Accepted(Call, Body);
            }
            catch (XdrException)
            {
                return Rpc.Error(Call.Xid, Rpc.AcceptGarbageArgs);
            }
            catch (NfsException E)
            {
                return Rpc.Accepted(Call.Xid, W =>
                {
                    W.WriteUInt32(E.Status);
                });
            }
            catch
            {
                return Rpc.Error(Call.Xid, Rpc.AcceptSystemError);
            }
        }

        private void GetAttr(RpcCall Call, XdrWriter W)
        {
            var Item = ReadItem(Call.Args);
            W.WriteUInt32(Nfs3Ok);
            WriteAttr(W, Item);
        }

        private void SetAttr(RpcCall Call, XdrWriter W)
        {
            var Handle = Call.Args.ReadOpaque(64);
            var Sattr = ReadSattr(Call.Args);
            bool Guarded = Call.Args.ReadBool();
            if (Guarded)
                Call.Args.ReadFixedOpaque(8);

            NfsItem Item = null;
            try
            {
                Item = Fs.GetFromHandle(Handle);
                Fs.SetAttributes(Item, Sattr.Mode, Sattr.User, Sattr.Group, Sattr.Size,
                    Sattr.SetModified, Sattr.Modified);
                W.WriteUInt32(Nfs3Ok);
                WriteWcc(W, Item, Item);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
                WriteWcc(W, Item, null);
            }
        }

        private void Lookup(RpcCall Call, XdrWriter W)
        {
            var Dir = ReadItem(Call.Args);
            var Name = Call.Args.ReadString(255);

            if (!Fs.IsDirectory(Dir))
            {
                W.WriteUInt32(Nfs3ErrNotDir);
                return;
            }

            try
            {
                NfsItem Target;
                if (Name == ".")
                    Target = Dir;
                else if (Name == "..")
                    Target = Fs.GetParent(Dir.Path) ?? Dir;
                else
                {
                    NfsFs.ValidateName(Name);
                    Target = Fs.Get(NfsFs.Combine(Dir.Path, Name));
                }

                W.WriteUInt32(Nfs3Ok);
                WritePostFh(W, Target.Path);
                WritePostAttr(W, Target);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
            }
        }

        private void Access(RpcCall Call, XdrWriter W)
        {
            var Item = ReadItem(Call.Args);
            uint Requested = Call.Args.ReadUInt32();
            W.WriteUInt32(Nfs3Ok);
            WritePostAttr(W, Item);
            W.WriteUInt32(Requested);
        }

        private void ReadLink(RpcCall Call, XdrWriter W)
        {
            var Item = ReadItem(Call.Args);
            if (Item.Item.ItemType != FilesystemItemType.SymLink)
            {
                W.WriteUInt32(Nfs3ErrInval);
                return;
            }

            W.WriteUInt32(Nfs3Ok);
            WritePostAttr(W, Item);
            W.WriteString(((SymLink)Item.Item).Target);
        }

        private void Read(RpcCall Call, XdrWriter W)
        {
            var Item = ReadItem(Call.Args);
            ulong Offset = Call.Args.ReadUInt64();
            uint Count = Call.Args.ReadUInt32();
            Count = Math.Min(Count, 32u * 1024u);

            try
            {
                var Data = Fs.ReadFile(Item, Offset, Count, out bool Eof);
                W.WriteUInt32(Nfs3Ok);
                WritePostAttr(W, Item);
                W.WriteUInt32((uint)Data.Length);
                W.WriteBool(Eof);
                W.WriteOpaque(Data);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
            }
        }

        private void Write(RpcCall Call, XdrWriter W)
        {
            var Handle = Call.Args.ReadOpaque(64);
            ulong Offset = Call.Args.ReadUInt64();
            uint Count = Call.Args.ReadUInt32();
            uint Stable = Call.Args.ReadUInt32();
            var Data = Call.Args.ReadOpaque(1024 * 1024);

            if (Data.Length != Count)
            {
                W.WriteUInt32(Nfs3ErrInval);
                WriteWcc(W, null, null);
                return;
            }

            NfsItem Item = null;
            try
            {
                Item = Fs.GetFromHandle(Handle);
                uint Written = Fs.WriteFile(Item, Offset, Data);
                W.WriteUInt32(Nfs3Ok);
                WriteWcc(W, Item, Item);
                W.WriteUInt32(Written);
                W.WriteUInt32(2);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
                WriteWcc(W, Item, null);
            }
        }

        private void Create(RpcCall Call, XdrWriter W)
        {
            var DirHandle = Call.Args.ReadOpaque(64);
            var Name = Call.Args.ReadString(255);
            uint How = Call.Args.ReadUInt32();
            if (How > 2)
            {
                W.WriteUInt32(Nfs3ErrInval);
                return;
            }

            var Sattr = (How == 2) ? new Sattr() : ReadSattr(Call.Args);
            if (How == 2)
                Call.Args.ReadFixedOpaque(8);

            try
            {
                var Dir = Fs.GetFromHandle(DirHandle);
                var Item = Fs.CreateFile(Dir.Path, Name, Sattr.Mode ?? 0x1A4, Sattr.User ?? 0, Sattr.Group ?? 0,
                    How != 0);
                W.WriteUInt32(Nfs3Ok);
                WritePostFh(W, Item.Path);
                WritePostAttr(W, Item);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
            }
        }

        private void MkDir(RpcCall Call, XdrWriter W)
        {
            var DirHandle = Call.Args.ReadOpaque(64);
            var Name = Call.Args.ReadString(255);
            var Sattr = ReadSattr(Call.Args);

            try
            {
                var Dir = Fs.GetFromHandle(DirHandle);
                var Item = Fs.CreateDirectory(Dir.Path, Name, Sattr.Mode ?? 0x1ED, Sattr.User ?? 0, Sattr.Group ?? 0);
                W.WriteUInt32(Nfs3Ok);
                WritePostFh(W, Item.Path);
                WritePostAttr(W, Item);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
            }
        }

        private void SymLink(RpcCall Call, XdrWriter W)
        {
            var DirHandle = Call.Args.ReadOpaque(64);
            var Name = Call.Args.ReadString(255);
            var Target = Call.Args.ReadString(4096);
            var Sattr = ReadSattr(Call.Args);

            try
            {
                var Dir = Fs.GetFromHandle(DirHandle);
                var Item = Fs.CreateSymLink(Dir.Path, Name, Target, Sattr.Mode ?? 0x1FF,
                    Sattr.User ?? 0, Sattr.Group ?? 0);
                W.WriteUInt32(Nfs3Ok);
                WritePostFh(W, Item.Path);
                WritePostAttr(W, Item);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
            }
        }

        private void MkNode(RpcCall Call, XdrWriter W)
        {
            Call.Args.ReadOpaque(64);
            Call.Args.ReadString(255);
            Call.Args.ReadUInt32();
            ReadSattr(Call.Args);
            W.WriteUInt32(Nfs3ErrNotSupp);
        }

        private void Remove(RpcCall Call, XdrWriter W, bool Directory)
        {
            var DirHandle = Call.Args.ReadOpaque(64);
            var Name = Call.Args.ReadString(255);

            NfsItem Dir = null;
            try
            {
                Dir = Fs.GetFromHandle(DirHandle);
                if (Directory)
                    Fs.RemoveDirectory(Dir.Path, Name);
                else
                    Fs.Remove(Dir.Path, Name);

                W.WriteUInt32(Nfs3Ok);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
            }

            WriteWcc(W, Dir, Dir);
        }

        private void Rename(RpcCall Call, XdrWriter W)
        {
            var FromHandle = Call.Args.ReadOpaque(64);
            var FromName = Call.Args.ReadString(255);
            var ToHandle = Call.Args.ReadOpaque(64);
            var ToName = Call.Args.ReadString(255);

            NfsItem FromDir = null;
            NfsItem ToDir = null;

            try
            {
                FromDir = Fs.GetFromHandle(FromHandle);
                ToDir = Fs.GetFromHandle(ToHandle);
                Fs.Rename(FromDir.Path, FromName, ToDir.Path, ToName, true);
                W.WriteUInt32(Nfs3Ok);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
            }

            WriteWcc(W, FromDir, FromDir);
            WriteWcc(W, ToDir, ToDir);
        }

        private void Link(RpcCall Call, XdrWriter W)
        {
            Call.Args.ReadOpaque(64);
            Call.Args.ReadOpaque(64);
            Call.Args.ReadString(255);
            W.WriteUInt32(Nfs3ErrNotSupp);
            WritePostAttr(W, null);
        }

        private void ReadDir(RpcCall Call, XdrWriter W, bool Plus)
        {
            var Item = ReadItem(Call.Args);
            ulong Cookie = Call.Args.ReadUInt64();
            Call.Args.ReadFixedOpaque(8);
            uint Count = Call.Args.ReadUInt32();
            if (Plus)
            {
                uint MaxCount = Call.Args.ReadUInt32();
                Count = Math.Min(Count, MaxCount);
            }
            Count = Math.Min(Count, 32u * 1024u);

            if (!Fs.IsDirectory(Item))
            {
                W.WriteUInt32(Nfs3ErrNotDir);
                return;
            }

            var Entries = new List<NfsDirEntry>
            {
                new NfsDirEntry(".", Item),
                new NfsDirEntry("..", Fs.GetParent(Item.Path) ?? Item)
            };

            foreach (var Child in ((Dir)Item.Item).Items)
                Entries.Add(new NfsDirEntry(Child.ShortFilename, new NfsItem
                {
                    Path = NfsFs.Combine(Item.Path, Child.ShortFilename),
                    Item = Child
                }));

            int Start = (Cookie == 0) ? 0 : (int)Math.Min(Cookie, (ulong)Entries.Count);
            var List = new XdrWriter();
            bool Eof = true;

            for (int i = Start; i < Entries.Count; i++)
            {
                var EntryWriter = new XdrWriter();
                WriteDirEntry(EntryWriter, Entries[i], (ulong)(i + 1), Plus);

                if ((List.Length > 0) && (List.Length + EntryWriter.Length + 8 > Count))
                {
                    Eof = false;
                    break;
                }

                List.WriteRaw(EntryWriter.ToArray());
            }

            List.WriteBool(false);
            List.WriteBool(Eof);

            W.WriteUInt32(Nfs3Ok);
            WritePostAttr(W, Item);
            W.WriteFixedOpaque(new byte[8]);
            W.WriteRaw(List.ToArray());
        }

        private void FsStat(RpcCall Call, XdrWriter W)
        {
            var Item = ReadItem(Call.Args);
            W.WriteUInt32(Nfs3Ok);
            WritePostAttr(W, Item);

            W.WriteUInt64(4UL * 1024 * 1024 * 1024);
            W.WriteUInt64(3UL * 1024 * 1024 * 1024);
            W.WriteUInt64(4UL * 1024 * 1024 * 1024);
            W.WriteUInt64(1024 * 1024);
            W.WriteUInt64(512 * 1024);
            W.WriteUInt64(1024 * 1024);
            W.WriteUInt32(0);
        }

        private void FsInfo(RpcCall Call, XdrWriter W)
        {
            var Item = ReadItem(Call.Args);
            W.WriteUInt32(Nfs3Ok);
            WritePostAttr(W, Item);

            W.WriteUInt32(32 * 1024);
            W.WriteUInt32(32 * 1024);
            W.WriteUInt32(4096);
            W.WriteUInt32(32 * 1024);
            W.WriteUInt32(32 * 1024);
            W.WriteUInt32(4096);
            W.WriteUInt32(8192);
            W.WriteUInt64(0x7FFFFFFFFFFFFFFF);
            W.WriteUInt32(0);
            W.WriteUInt32(1);
            W.WriteUInt32(1 | 2 | 8 | 16);
        }

        private void PathConf(RpcCall Call, XdrWriter W)
        {
            var Item = ReadItem(Call.Args);
            W.WriteUInt32(Nfs3Ok);
            WritePostAttr(W, Item);
            W.WriteUInt32(32000);
            W.WriteUInt32(255);
            W.WriteBool(true);
            W.WriteBool(false);
            W.WriteBool(false);
            W.WriteBool(true);
        }

        private void Commit(RpcCall Call, XdrWriter W)
        {
            var Handle = Call.Args.ReadOpaque(64);
            Call.Args.ReadUInt64();
            Call.Args.ReadUInt32();

            NfsItem Item = null;
            try
            {
                Item = Fs.GetFromHandle(Handle);
                W.WriteUInt32(Nfs3Ok);
                WriteWcc(W, Item, Item);
                W.WriteFixedOpaque(new byte[8]);
            }
            catch (NfsException E)
            {
                W.WriteUInt32(E.Status);
                WriteWcc(W, null, null);
            }
        }

        private NfsItem ReadItem(XdrReader Reader)
        {
            var Handle = Reader.ReadOpaque(64);
            return Fs.GetFromHandle(Handle);
        }

        private static Sattr ReadSattr(XdrReader Reader)
        {
            var Result = new Sattr();

            if (Reader.ReadBool())
                Result.Mode = Reader.ReadUInt32();

            if (Reader.ReadBool())
                Result.User = Reader.ReadUInt32();

            if (Reader.ReadBool())
                Result.Group = Reader.ReadUInt32();

            if (Reader.ReadBool())
                Result.Size = Reader.ReadUInt64();

            uint Atime = Reader.ReadUInt32();
            if (Atime == TimeClient)
                Reader.ReadFixedOpaque(8);

            uint Mtime = Reader.ReadUInt32();
            if (Mtime == TimeClient)
            {
                uint Seconds = Reader.ReadUInt32();
                Reader.ReadUInt32();
                Result.SetModified = true;
                Result.Modified = Seconds;
            }
            else if (Mtime == TimeServer)
            {
                Result.SetModified = true;
                Result.Modified = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            }

            return Result;
        }

        private void WriteAttr(XdrWriter W, NfsItem Item)
        {
            var I = Item.Item;
            uint Type = TypeOf(I.ItemType);
            uint Nlink = 1;
            if (I.ItemType == FilesystemItemType.Directory)
            {
                Nlink = 2;
                foreach (var Child in ((Dir)I).Items)
                    if (Child.ItemType == FilesystemItemType.Directory)
                        Nlink++;
            }

            ulong Size = (ulong)Math.Max(0, I.Size);
            ulong Used = (I.ItemType == FilesystemItemType.Directory) ? 4096UL : Size;

            W.WriteUInt32(Type);
            W.WriteUInt32(I.FullMode);
            W.WriteUInt32(Nlink);
            W.WriteUInt32(I.User);
            W.WriteUInt32(I.Group);
            W.WriteUInt64(Size);
            W.WriteUInt64(Used);
            W.WriteUInt32(0);
            W.WriteUInt32(0);
            W.WriteUInt64(FsId);
            W.WriteUInt64(Fs.Handles.GetId(Item.Path));
            WriteTime(W, Unix(I.Modified));
            WriteTime(W, Unix(I.Modified));
            WriteTime(W, Unix(I.Created > new DateTime(1970, 1, 2) ? I.Created : I.Modified));
        }

        private void WritePostAttr(XdrWriter W, NfsItem Item)
        {
            if (Item == null)
            {
                W.WriteBool(false);
                return;
            }

            W.WriteBool(true);
            WriteAttr(W, Item);
        }

        private void WritePostFh(XdrWriter W, string Path)
        {
            W.WriteBool(true);
            W.WriteOpaque(Fs.Handles.Get(Path));
        }

        private void WriteWcc(XdrWriter W, NfsItem Before, NfsItem After)
        {
            if (Before == null)
                W.WriteBool(false);
            else
            {
                W.WriteBool(true);
                W.WriteUInt64((ulong)Math.Max(0, Before.Item.Size));
                WriteTime(W, Unix(Before.Item.Modified));
                WriteTime(W, Unix(Before.Item.Created > new DateTime(1970, 1, 2) ? Before.Item.Created : Before.Item.Modified));
            }

            WritePostAttr(W, After);
        }

        private void WriteDirEntry(XdrWriter W, NfsDirEntry Entry, ulong Cookie, bool Plus)
        {
            W.WriteBool(true);
            W.WriteUInt64(Fs.Handles.GetId(Entry.Item.Path));
            W.WriteString(Entry.Name);
            W.WriteUInt64(Cookie);

            if (Plus)
            {
                WritePostAttr(W, Entry.Item);
                WritePostFh(W, Entry.Item.Path);
            }
        }

        private static void WriteTime(XdrWriter W, long Seconds)
        {
            W.WriteUInt32((uint)Math.Max(0, Seconds));
            W.WriteUInt32(0);
        }

        private static long Unix(DateTime Time)
        {
            if (Time <= new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                return 0;

            return (long)(Time.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        private static uint TypeOf(FilesystemItemType Type)
        {
            switch (Type)
            {
                case FilesystemItemType.File: return Nf3Reg;
                case FilesystemItemType.Directory: return Nf3Dir;
                case FilesystemItemType.Block: return Nf3Blk;
                case FilesystemItemType.Character: return Nf3Chr;
                case FilesystemItemType.SymLink: return Nf3Lnk;
                case FilesystemItemType.Socket: return Nf3Sock;
                case FilesystemItemType.Fifo: return Nf3Fifo;
                default: return Nf3Reg;
            }
        }

        private sealed class Sattr
        {
            public uint? Mode;
            public uint? User;
            public uint? Group;
            public ulong? Size;
            public bool SetModified;
            public long Modified;
        }

        private sealed class NfsDirEntry
        {
            public readonly string Name;
            public readonly NfsItem Item;

            public NfsDirEntry(string Name, NfsItem Item)
            {
                this.Name = Name;
                this.Item = Item;
            }
        }
    }
}

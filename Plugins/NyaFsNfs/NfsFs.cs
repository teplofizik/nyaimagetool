using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.Filesystem.Universal.Types;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.Processor;
using System;

namespace NyaFsNfs
{
    internal sealed class NfsException : Exception
    {
        public uint Status { get; }

        public NfsException(uint Status, string Message = null) : base(Message ?? $"NFS error {Status}")
        {
            this.Status = Status;
        }
    }

    internal sealed class NfsItem
    {
        public string Path;
        public FilesystemItem Item;
    }

    internal sealed class NfsFs
    {
        public const uint Ok = 0;
        public const uint ErrNoEnt = 2;
        public const uint ErrIO = 5;
        public const uint ErrAcces = 13;
        public const uint ErrExist = 17;
        public const uint ErrNotDir = 20;
        public const uint ErrIsDir = 21;
        public const uint ErrInval = 22;
        public const uint ErrNameTooLong = 63;
        public const uint ErrNotEmpty = 66;
        public const uint ErrStale = 70;
        public const uint ErrBadHandle = 10001;
        public const uint ErrNotSupp = 10004;

        private readonly ImageProcessor Processor;

        public NfsHandles Handles { get; } = new NfsHandles();

        public object SyncRoot { get; } = new object();

        public NfsFs(ImageProcessor Processor)
        {
            this.Processor = Processor;
        }

        public LinuxFilesystem GetFilesystem()
        {
            var Fs = Processor?.GetFs();
            if (Fs == null)
                throw new NfsException(ErrStale, "Filesystem is not loaded");

            return Fs;
        }

        public NfsItem GetRoot()
        {
            var Fs = GetFilesystem();
            return new NfsItem { Path = "", Item = Fs.Fs.Root };
        }

        public NfsItem Get(string Path)
        {
            var Fs = GetFilesystem();
            if (Path.Length == 0)
                return new NfsItem { Path = "", Item = Fs.Fs.Root };

            if (!Fs.Exists("/" + Path))
                throw new NfsException(ErrNoEnt);

            return new NfsItem { Path = Path, Item = Fs.GetElement("/" + Path) };
        }

        public NfsItem TryGet(string Path)
        {
            try
            {
                return Get(Path);
            }
            catch (NfsException E) when (E.Status == ErrNoEnt)
            {
                return null;
            }
        }

        public NfsItem GetParent(string Path)
        {
            if (Path.Length == 0)
                return null;

            var Parent = ParentPath(Path);
            return Get(Parent);
        }

        public NfsItem GetFromHandle(byte[] Handle)
        {
            if (!Handles.TryResolve(Handle, out string Path))
                throw new NfsException(ErrBadHandle);

            return Get(Path);
        }

        public bool IsDirectory(NfsItem Item) => Item.Item.ItemType == FilesystemItemType.Directory;

        public NfsItem CreateFile(string Directory, string Name, uint Mode, uint User, uint Group, bool Guarded)
        {
            ValidateName(Name);
            var Dir = Get(Directory);
            if (!IsDirectory(Dir))
                throw new NfsException(ErrNotDir);

            var Path = Combine(Directory, Name);
            var Existing = TryGet(Path);
            if (Existing != null)
            {
                if (Guarded)
                    throw new NfsException(ErrExist);

                if (IsDirectory(Existing))
                    throw new NfsException(ErrIsDir);

                return Existing;
            }

            var Item = new File(Path, User, Group, Mode & 0xFFF, Array.Empty<byte>());
            ((Dir)Dir.Item).Items.Add(Item);
            Handles.Get(Path);
            return new NfsItem { Path = Path, Item = Item };
        }

        public NfsItem CreateDirectory(string Directory, string Name, uint Mode, uint User, uint Group)
        {
            ValidateName(Name);
            var Dir = Get(Directory);
            if (!IsDirectory(Dir))
                throw new NfsException(ErrNotDir);

            var Path = Combine(Directory, Name);
            if (TryGet(Path) != null)
                throw new NfsException(ErrExist);

            var Item = new Dir(Path, User, Group, Mode & 0xFFF);
            ((Dir)Dir.Item).Items.Add(Item);
            Handles.Get(Path);
            return new NfsItem { Path = Path, Item = Item };
        }

        public NfsItem CreateSymLink(string Directory, string Name, string Target, uint Mode, uint User, uint Group)
        {
            ValidateName(Name);
            var Dir = Get(Directory);
            if (!IsDirectory(Dir))
                throw new NfsException(ErrNotDir);

            var Path = Combine(Directory, Name);
            if (TryGet(Path) != null)
                throw new NfsException(ErrExist);

            var Item = new SymLink(Path, User, Group, Mode & 0xFFF, Target);
            ((Dir)Dir.Item).Items.Add(Item);
            Handles.Get(Path);
            return new NfsItem { Path = Path, Item = Item };
        }

        public void Remove(string Directory, string Name)
        {
            ValidateName(Name);
            var Dir = Get(Directory);
            if (!IsDirectory(Dir))
                throw new NfsException(ErrNotDir);

            var Path = Combine(Directory, Name);
            var Target = Get(Path);
            if (IsDirectory(Target))
                throw new NfsException(ErrIsDir);

            ((Dir)Dir.Item).Items.Remove(Target.Item);
            Handles.Remove(Path);
        }

        public void RemoveDirectory(string Directory, string Name)
        {
            ValidateName(Name);
            var Dir = Get(Directory);
            if (!IsDirectory(Dir))
                throw new NfsException(ErrNotDir);

            var Path = Combine(Directory, Name);
            var Target = Get(Path);
            if (!IsDirectory(Target))
                throw new NfsException(ErrNotDir);

            if (((Dir)Target.Item).Items.Count != 0)
                throw new NfsException(ErrNotEmpty);

            ((Dir)Dir.Item).Items.Remove(Target.Item);
            Handles.Remove(Path);
        }

        public void Rename(string FromDirectory, string FromName, string ToDirectory, string ToName, bool Replace)
        {
            ValidateName(FromName);
            ValidateName(ToName);

            var FromDir = Get(FromDirectory);
            var ToDir = Get(ToDirectory);
            if (!IsDirectory(FromDir) || !IsDirectory(ToDir))
                throw new NfsException(ErrNotDir);

            var FromPath = Combine(FromDirectory, FromName);
            var ToPath = Combine(ToDirectory, ToName);
            var Source = Get(FromPath);

            var Existing = TryGet(ToPath);
            if (Existing != null)
            {
                if (!Replace)
                    throw new NfsException(ErrExist);

                if (IsDirectory(Existing) && ((Dir)Existing.Item).Items.Count != 0)
                    throw new NfsException(ErrNotEmpty);

                ((Dir)ToDir.Item).Items.Remove(Existing.Item);
                Handles.Remove(ToPath);
            }

            if (FromDir != ToDir)
                ((Dir)FromDir.Item).Items.Remove(Source.Item);

            if (FromDir != ToDir)
                ((Dir)ToDir.Item).Items.Add(Source.Item);

            Source.Item.Filename = ToPath;
            Handles.Rename(FromPath, ToPath);
        }

        public byte[] ReadFile(NfsItem Item, ulong Offset, uint Count, out bool Eof)
        {
            if (Item.Item.ItemType == FilesystemItemType.Directory)
                throw new NfsException(ErrIsDir);

            if (Item.Item.ItemType != FilesystemItemType.File)
                throw new NfsException(ErrInval);

            var Content = ((File)Item.Item).Content ?? Array.Empty<byte>();
            if (Offset >= (ulong)Content.Length)
            {
                Eof = true;
                return Array.Empty<byte>();
            }

            int Start = (int)Offset;
            int Available = Content.Length - Start;
            int Length = (int)Math.Min((ulong)Available, (ulong)Count);
            var Result = new byte[Length];
            Array.Copy(Content, Start, Result, 0, Length);
            Eof = (Start + Length) >= Content.Length;
            return Result;
        }

        public uint WriteFile(NfsItem Item, ulong Offset, byte[] Data)
        {
            if (Item.Item.ItemType == FilesystemItemType.Directory)
                throw new NfsException(ErrIsDir);

            if (Item.Item.ItemType != FilesystemItemType.File)
                throw new NfsException(ErrInval);

            var FileItem = (File)Item.Item;
            var Content = FileItem.Content ?? Array.Empty<byte>();
            ulong Required = Offset + (ulong)Data.Length;
            if (Required > Int32.MaxValue)
                throw new NfsException(ErrInval);

            int Size = (int)Required;
            if (Size > Content.Length)
            {
                var NewContent = new byte[Size];
                Array.Copy(Content, NewContent, Content.Length);
                Content = NewContent;
            }

            Array.Copy(Data, 0, Content, (int)Offset, Data.Length);
            FileItem.Content = Content;
            FileItem.Modified = DateTime.UtcNow;
            return (uint)Data.Length;
        }

        public void SetAttributes(NfsItem Item, uint? Mode, uint? User, uint? Group, ulong? Size,
            bool SetModified, long ModifiedSeconds)
        {
            if (Mode.HasValue)
                Item.Item.Mode = Mode.Value & 0xFFF;

            if (User.HasValue)
                Item.Item.User = User.Value;

            if (Group.HasValue)
                Item.Item.Group = Group.Value;

            if (Size.HasValue && (Item.Item.ItemType == FilesystemItemType.File))
            {
                if (Size.Value > Int32.MaxValue)
                    throw new NfsException(ErrInval);

                var FileItem = (File)Item.Item;
                var Content = FileItem.Content ?? Array.Empty<byte>();
                int NewSize = (int)Size.Value;
                if (NewSize != Content.Length)
                {
                    var NewContent = new byte[NewSize];
                    Array.Copy(Content, NewContent, Math.Min(Content.Length, NewSize));
                    FileItem.Content = NewContent;
                }
            }

            if (SetModified)
                Item.Item.Modified = NyaFs.Filesystem.Universal.Helper.FsHelper.ConvertFromUnixTimestamp(ModifiedSeconds);
        }

        public static string Combine(string Directory, string Name) =>
            (Directory.Length == 0) ? Name : Directory + "/" + Name;

        public static string ParentPath(string Path)
        {
            int Slash = Path.LastIndexOf('/');
            return (Slash < 0) ? "" : Path.Substring(0, Slash);
        }

        public static string NamePath(string Path)
        {
            int Slash = Path.LastIndexOf('/');
            return (Slash < 0) ? Path : Path.Substring(Slash + 1);
        }

        public static void ValidateName(string Name)
        {
            if (String.IsNullOrEmpty(Name))
                throw new NfsException(ErrInval, "Empty name");

            if ((Name == ".") || (Name == ".."))
                throw new NfsException(ErrInval, "Invalid name");

            if (Name.IndexOf('/') >= 0)
                throw new NfsException(ErrInval, "Name contains '/'");

            if (Name.Length > 255)
                throw new NfsException(ErrNameTooLong);
        }
    }
}

using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.Filesystem.Universal.Types;
using NyaFs.ImageFormat.Elements.Fs;
using DirItem = NyaFs.Filesystem.Universal.Items.Dir;
using FileItem = NyaFs.Filesystem.Universal.Items.File;
using SymLinkItem = NyaFs.Filesystem.Universal.Items.SymLink;
using System;
using System.IO;

namespace NyaFsRemoteCore
{
    public static class RemoteTransfer
    {
        private static string CombinePath(string Base, string Name) =>
            (Base == "/" || Base == ".") ? Name : Base + "/" + Name;

        private static string CombineRemote(string Path, string Name) =>
            (Path.EndsWith("/") ? Path : Path + "/") + Name;

        private static string ParentRemote(string Path)
        {
            var P = Path.TrimEnd('/');
            int Slash = P.LastIndexOf('/');
            return (Slash <= 0) ? "/" : P.Substring(0, Slash);
        }

        public static void Download(IRemoteFs Remote, string RemotePath, LinuxFilesystem Fs, string FsPath)
        {
            var Entry = Remote.GetEntry(RemotePath);
            if (Entry == null)
                throw new IOException($"Remote path '{RemotePath}' not found");

            if (Entry.IsDirectory)
            {
                EnsureFsDirectory(Fs, FsPath, Entry.Mode, Entry.User, Entry.Group);
                foreach (var Child in Remote.List(RemotePath))
                    Download(Remote, CombineRemote(RemotePath, Child.Name), Fs, FsPath.TrimEnd('/') + "/" + Child.Name);

                return;
            }

            if (Entry.IsSymLink)
            {
                if (string.IsNullOrEmpty(Entry.LinkTarget))
                {
                    NyaFs.Log.Warning(0, $"Skipping symlink '{RemotePath}': target is not available");
                    return;
                }

                AddOrUpdateSymLink(Fs, FsPath, Entry.LinkTarget, Entry.Mode, Entry.User, Entry.Group);
                return;
            }

            var Data = Remote.ReadFile(RemotePath);
            AddOrUpdateFile(Fs, FsPath, Data, Entry.Mode, Entry.User, Entry.Group);
        }

        public static void Upload(LinuxFilesystem Fs, string FsPath, IRemoteFs Remote, string RemotePath)
        {
            var Item = Fs.GetElement(FsPath);
            if (Item == null)
                throw new IOException($"Filesystem path '{FsPath}' not found");

            if (Item is DirItem Dir)
            {
                Remote.CreateDirectory(RemotePath);
                foreach (var Child in Dir.Items)
                {
                    if (Child is DirItem || Child is FileItem)
                        Upload(Fs, Child.Filename, Remote, CombineRemote(RemotePath, Child.ShortFilename));
                    else
                        NyaFs.Log.Warning(0, $"Skipping unsupported item '{Child.Filename}'");
                }

                return;
            }

            if (Item is FileItem File)
            {
                Remote.CreateDirectory(ParentRemote(RemotePath));
                Remote.WriteFile(RemotePath, File.Content ?? Array.Empty<byte>());
                return;
            }

            throw new IOException($"Unsupported filesystem item '{FsPath}' ({Item.ItemType})");
        }

        private static void EnsureFsDirectory(LinuxFilesystem Fs, string Path, uint Mode, uint User, uint Group)
        {
            var Normalized = Path.Trim('/');
            if (Normalized.Length == 0)
                return;

            if (Fs.Exists(Normalized))
            {
                var Existing = Fs.GetElement(Normalized);
                if (Existing is DirItem)
                    return;

                throw new IOException($"'{Normalized}' exists in filesystem and is not a directory");
            }

            EnsureParentDirectory(Fs, Normalized);

            var Parent = Fs.GetParentDirectory(Normalized);
            if (Parent == null)
                throw new IOException($"Parent directory for '{Normalized}' not found");

            Parent.Items.Add(new DirItem(Normalized, User, Group, Mode & 0xFFF));
        }

        private static void EnsureParentDirectory(LinuxFilesystem Fs, string Path)
        {
            var Parts = Path.Split('/');
            var Current = "/";

            for (int i = 0; i < Parts.Length - 1; i++)
            {
                var Part = Parts[i];
                if (Part.Length == 0)
                    continue;

                Current = CombinePath(Current, Part);

                if (!Fs.Exists(Current))
                {
                    var Parent = Fs.GetParentDirectory(Current);
                    if (Parent == null)
                        throw new IOException($"Cannot create parent directory '{Current}'");

                    Parent.Items.Add(new DirItem(Current, 0, 0, 0x1ED));
                }
                else if (!(Fs.GetElement(Current) is DirItem))
                    throw new IOException($"'{Current}' is not a directory");
            }
        }

        private static void AddOrUpdateFile(LinuxFilesystem Fs, string Path, byte[] Data, uint Mode, uint User, uint Group)
        {
            var Normalized = Path.Trim('/');
            if (Fs.Exists(Normalized))
            {
                if (Fs.GetElement(Normalized) is FileItem Existing)
                {
                    Existing.Mode = Mode & 0xFFF;
                    Existing.User = User;
                    Existing.Group = Group;
                    Existing.Content = Data;
                    Existing.Modified = DateTime.Now;
                    return;
                }

                throw new IOException($"'{Normalized}' exists in filesystem and is not a file");
            }

            EnsureParentDirectory(Fs, Normalized);

            var Parent = Fs.GetParentDirectory(Normalized);
            if (Parent == null)
                throw new IOException($"Parent directory for '{Normalized}' not found");

            Parent.Items.Add(new FileItem(Normalized, User, Group, Mode & 0xFFF, Data));
        }

        private static void AddOrUpdateSymLink(LinuxFilesystem Fs, string Path, string Target, uint Mode, uint User, uint Group)
        {
            var Normalized = Path.Trim('/');
            if (Fs.Exists(Normalized))
            {
                var Existing = Fs.GetElement(Normalized);
                if (Existing is SymLinkItem ExistingLink)
                {
                    ExistingLink.Target = Target;
                    ExistingLink.Mode = Mode & 0xFFF;
                    ExistingLink.User = User;
                    ExistingLink.Group = Group;
                    return;
                }

                var ExistingParent = Fs.GetParentDirectory(Normalized);
                ExistingParent?.Items.Remove(Existing);
            }

            EnsureParentDirectory(Fs, Normalized);

            var Parent = Fs.GetParentDirectory(Normalized);
            if (Parent == null)
                throw new IOException($"Parent directory for '{Normalized}' not found");

            Parent.Items.Add(new SymLinkItem(Normalized, User, Group, Mode & 0xFFF, Target));
        }
    }
}

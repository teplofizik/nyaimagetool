using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.ImageFormat.Elements.Fs;
using System;

namespace NyaFsLinux.Linux.Users
{
    internal static class Skeleton
    {
        public static void Copy(LinuxFilesystem Fs, string Source, string Target, uint Uid, uint Gid)
        {
            if (!Fs.Exists(Source) || !Fs.Exists(Target))
                return;

            var SourceDir = Fs.GetElement(Source) as Dir;
            var TargetDir = Fs.GetElement(Target) as Dir;
            if ((SourceDir == null) || (TargetDir == null))
                return;

            foreach (var Item in SourceDir.Items)
                CopyItem(Fs, Item, Target.TrimEnd('/') + "/" + Item.ShortFilename, Uid, Gid, TargetDir);
        }

        private static void CopyItem(LinuxFilesystem Fs, FilesystemItem Item, string Path, uint Uid, uint Gid, Dir Parent)
        {
            if (Fs.Exists(Path))
                return;

            if (Item is Dir)
            {
                var Dir = new Dir(Path, Uid, Gid, Item.Mode);
                Parent.Items.Add(Dir);
                foreach (var Child in ((Dir)Item).Items)
                    CopyItem(Fs, Child, Path.TrimEnd('/') + "/" + Child.ShortFilename, Uid, Gid, Dir);
            }
            else if (Item is File)
                Parent.Items.Add(new File(Path, Uid, Gid, Item.Mode, ((File)Item).Content));
            else if (Item is SymLink)
                Parent.Items.Add(new SymLink(Path, Uid, Gid, Item.Mode, ((SymLink)Item).Target));
        }
    }
}

using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs
{
    // cpio
    // gzipped cpio
    // ext4
    // gzipped ext4
    // legacy gzipped cpio
    // legacy gzipped ext4
    // fit => gzipped cpio
    // fit => gzipped ext4

    public class LinuxFilesystem
    {
        public NyaFs.Filesystem.Universal.Filesystem Fs = new NyaFs.Filesystem.Universal.Filesystem();

        /// <summary>
        /// Filesystem type
        /// </summary>
        public Types.FsType FilesystemType = Types.FsType.Unknown;

        /// <summary>
        /// Image information, arch or supported os
        /// </summary>
        public Types.ImageInfo Info = new Types.ImageInfo();

        /// <summary>
        /// Is image loaded?
        /// </summary>
        public bool Loaded => Fs.Loaded;

        public void Dump() => Fs.Dump();

        public Dir GetDirectory(string Path) => Fs.GetDirectory(Path);

        public Dir GetParentDirectory(string Path) => Fs.GetParentDirectory(Path);

        public bool Exists(string Path) => Fs.Exists(Path);

        public FilesystemItem GetElement(string Path) => Fs.GetElement(Path);

        public void Delete(string Path) => Fs.Delete(Path);

        public long GetContentSize() => GetDirSize(Fs.Root);

        private long GetDirSize(Filesystem.Universal.Items.Dir Dir)
        {
            long Res = 0;

            foreach (var I in Dir.Items)
            {
                switch(I.ItemType)
                {
                    case Filesystem.Universal.Types.FilesystemItemType.File:
                    case Filesystem.Universal.Types.FilesystemItemType.SymLink:
                        Res += I.Size;
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Directory:
                        Res += GetDirSize(I as Filesystem.Universal.Items.Dir);
                        break;
                }
            }

            return Res;
        }
    }
}

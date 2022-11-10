using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs
{

    public class LinuxFilesystem
    {
        public NyaFs.Filesystem.Universal.Filesystem Fs = new NyaFs.Filesystem.Universal.Filesystem();

        /// <summary>
        /// Filesystem type
        /// </summary>
        public Filesystem.SquashFs.Types.SqCompressionType SquashFsCompression = Filesystem.SquashFs.Types.SqCompressionType.Gzip;

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

        /// <summary>
        /// Delete specified element from filesystem
        /// </summary>
        /// <param name="Path">Path to delete</param>
        public void Delete(string Path) => Fs.Delete(Path);

        /// <summary>
        /// Calculate total amount of data over all files on filesystem (only content of regular files + text of symlinks)
        /// </summary>
        /// <returns>Total amount of data in filesystem</returns>
        public long GetContentSize() => GetDirSize(Fs.Root);

        /// <summary>
        /// Calculate total amount of data over all files in directory (only content of regular files + text of symlinks)
        /// </summary>
        /// <param name="Dir">Directory to calc sum of files size</param>
        /// <returns>Total amount of data in deirectory</returns>
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

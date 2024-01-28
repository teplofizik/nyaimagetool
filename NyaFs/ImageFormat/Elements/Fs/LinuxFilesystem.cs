using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Items;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NyaFs.ImageFormat.Elements.Fs
{

    public class LinuxFilesystem
    {
        public Filesystem.Universal.Filesystem Fs = new Filesystem.Universal.Filesystem();

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
        public bool Loaded => Fs.Loaded || (FilesystemType != Types.FsType.Unknown);

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
        /// Search filesystem items in dir by path with mask
        /// </summary>
        /// <param name="Dir">Directory for search</param>
        /// <param name="Mask">Search mask</param>
        /// <returns></returns>
        public string[] Search(Dir Dir, string Mask)
        {
            string pattern = "^" + Regex.Escape(Mask).Replace("\\*", ".*");
            
            return Search(Dir, I => Regex.IsMatch(I.Filename, pattern));
        }

        /// <summary>
        /// Search filesystem items by path with mask
        /// </summary>
        /// <param name="Mask"></param>
        /// <returns></returns>
        public string[] Search(string Mask) => Search(Fs.Root, Mask);

        /// <summary>
        /// Search elements by path
        /// </summary>
        /// <param name="Dir">Directory for search</param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        public string[] Search(Dir Dir, Func<FilesystemItem,bool> Condition)
        {
            var Res = new List<string>();

            foreach (var I in Dir.Items)
            {
                switch (I.ItemType)
                {
                    case Filesystem.Universal.Types.FilesystemItemType.File:
                    case Filesystem.Universal.Types.FilesystemItemType.SymLink:
                        if (Condition(I))
                            Res.Add(I.Filename);
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Directory:
                        if (Condition(I))
                            Res.Add(I.Filename);
                        else
                            Res.AddRange(Search(I as Dir, Condition));
                        break;
                }
            }

            return Res.ToArray();
        }

        /// <summary>
        /// Calculate total amount of data over all files in directory (only content of regular files + text of symlinks)
        /// </summary>
        /// <param name="Dir">Directory to calc sum of files size</param>
        /// <returns>Total amount of data in deirectory</returns>
        private long GetDirSize(Dir Dir)
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
                        Res += GetDirSize(I as Dir);
                        break;
                }
            }

            return Res;
        }
    }
}

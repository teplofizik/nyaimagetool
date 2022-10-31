using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.Base
{
    /// <summary>
    /// Base filesystem plugin
    /// </summary>
    public class FilesystemPlugin : NyaPlugin
    {
        public readonly string FilesystemName;

        private bool Write;
        private bool Read;
        private bool Compression;

        public FilesystemPlugin(string Name, string FilesystemName, bool Read, bool Write, bool InternalCompression) : base(Name, "filesystem")
        {
            this.FilesystemName = FilesystemName;

            this.Read = Read;
            this.Write = Write;

            Compression = InternalCompression;
        }

        /// <summary>
        /// Is filesystem need compression in target image
        /// </summary>
        public virtual bool NeedExternalCompression => !Compression;

        /// <summary>
        /// Is read supported
        /// </summary>
        public virtual bool SupportRead => Read;

        /// <summary>
        /// Is write supported
        /// </summary>
        public virtual bool SupportWrite => Write;

        /// <summary>
        /// Is image contain this filesystem
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public virtual bool DetectFilesystem(byte[] Image) => false;

        /// <summary>
        /// Get reader
        /// </summary>
        /// <returns></returns>
        public virtual Elements.Fs.Reader.Reader GetFilesystemReader(byte[] Data) => null;

        /// <summary>
        /// Get writer
        /// </summary>
        /// <returns></returns>
        public virtual Elements.Fs.Writer.Writer GetFilesystemWriter(Elements.Fs.LinuxFilesystem Fs) => null;

        /// <summary>
        /// Check is filesystem can save all data
        /// </summary>
        /// <param name="Fs"></param>
        /// <returns></returns>

        public virtual bool CheckFilesystem(Elements.Fs.LinuxFilesystem Fs) => true;

        protected void IterateAllFilesystem(Elements.Fs.LinuxFilesystem Fs, Action<Filesystem.Universal.FilesystemItem> OnItem)
        {
            IterateDirectory(Fs.Fs.Root, OnItem);
        }

        private void IterateDirectory(Filesystem.Universal.Items.Dir Dir, Action<Filesystem.Universal.FilesystemItem> OnItem)
        {
            OnItem(Dir);

            foreach(var N in Dir.Items)
            {
                if (N.ItemType == Filesystem.Universal.Types.FilesystemItemType.Directory)
                    IterateDirectory(N as Filesystem.Universal.Items.Dir, OnItem);
                else
                    OnItem(N);
            }
        }
    }
}

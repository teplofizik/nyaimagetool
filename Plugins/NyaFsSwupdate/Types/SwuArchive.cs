using NyaFs.Filesystem.Cpio.Types;
using NyaFs.Filesystem.Cpio;
using NyaIO.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Enumeration;

namespace NyaFsSwupdate.Types
{
    public class SwuArchive : CpioFsReader
    {
        private NyaFs.Filesystem.Universal.FilesystemEntry[] Entries;

        public SwuArchive(byte[] bytes) : base(bytes, "070702")
        {
            Entries = ReadDir("/");
        }

        public bool Correct => Entries.Length > 0;

        public byte[] Rootfs => Read("rootfs.ubi");
        public byte[] Devtree => Read("devicetree.dtb");
        public byte[] Kernel => Read("uImage");
    }
}

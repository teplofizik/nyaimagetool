using System;
using System.IO;
using SharpCompress.Compressors.Xz;

namespace NyaFs.Filesystem.SquashFs.Compression
{
    internal class Xz : BaseCompressor
    {
        internal Xz()
        {

        }

        internal Xz(byte[] Raw, long Offset) : base(Raw, Offset, 8)
        {

        }

        /// <summary>
        /// Should be > 8KiB, and must be either the sum of a power of two, or the sum of two sequential powers of two (2n or 2n + 2n+1)
        /// u32 dictionary_size (0x00)
        /// </summary>
        internal uint DictionarySize
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        /// <summary>
        /// A bitfield describing the additional enabled filters attempted to better compress executable code. Flags:
        ///  0x01: x86
        ///  0x02: powerpc
        ///  0x04: ia64
        ///  0x08: arm
        ///  0x10: armthumb
        ///  0x20: sparc
        /// u32 executable_filters (0x04)
        /// </summary>
        internal ExecutableFilters ExecFilters
        {
            get { return (ExecutableFilters)ReadUInt32(4); }
            set { WriteUInt32(4, Convert.ToUInt32(value)); }
        }

        internal override byte[] Compress(byte[] data)
        {
            throw new NotImplementedException();
        }

        internal override byte[] Decompress(byte[] data)
        {
            using (MemoryStream input = new MemoryStream(data))
            {
                using (XZStream xz = new XZStream(input))
                {
                    using (var output = new MemoryStream())
                    {
                        xz.CopyTo(output);

                        return output.ToArray();
                    }
                }
            }
        }

        [Flags]
        internal enum ExecutableFilters
        {
            x86 = 0x01,
            powerpc = 0x02,
            ia64 = 0x04,
            arm = 0x08,
            armthumb = 0x10,
            sparc = 0x20
        }
    }
}

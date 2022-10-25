using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.CompressorPlugins
{
    class Bz2CompressorPlugin : Base.CompressorPlugin
    {

        public Bz2CompressorPlugin() : base("compr.bz2", "bz2", true, true) { }

        /// <summary>
        /// Is image contain this filesystem
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public override bool DetectCompressor(byte[] Image) => false;

        /// <summary>
        /// Decompress
        /// </summary>
        /// <returns></returns>
        public override byte[] Decompress(byte[] Data) => Compressors.Bzip2.Decompress(Data);

        /// <summary>
        /// Compress
        /// </summary>
        /// <returns></returns>
        public override byte[] Compress(byte[] Data) => Compressors.Bzip2.CompressWithHeader(Data);
    }
}

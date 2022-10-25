using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.CompressorPlugins
{
    class Lz4CompressorPlugin : Base.CompressorPlugin
    {

        public Lz4CompressorPlugin() : base("compr.lz4", "lz4", true, true) { }

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
        public override byte[] Decompress(byte[] Data) => Compressors.Lz4.Decompress(Data);

        /// <summary>
        /// Compress
        /// </summary>
        /// <returns></returns>
        public override byte[] Compress(byte[] Data) => Compressors.Lz4.CompressWithHeader(Data);
    }
}

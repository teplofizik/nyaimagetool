using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.CompressorPlugins
{
    class ZStdCompressorPlugin : Base.CompressorPlugin
    {

        public ZStdCompressorPlugin() : base("compr.zstd", "zstd", true, true) { }

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
        public override byte[] Decompress(byte[] Data) => Compressors.ZStd.Decompress(Data);

        /// <summary>
        /// Compress 
        /// </summary>
        /// <returns></returns>
        public override byte[] Compress(byte[] Data) => Compressors.ZStd.CompressWithHeader(Data);
    }
}

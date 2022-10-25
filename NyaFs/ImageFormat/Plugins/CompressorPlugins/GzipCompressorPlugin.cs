using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.CompressorPlugins
{
    class GzipCompressorPlugin : Base.CompressorPlugin
    {

        public GzipCompressorPlugin() : base("compr.gzip", "gzip", true, true) { }

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
        public override byte[] Decompress(byte[] Data) => Compressors.Gzip.Decompress(Data);

        /// <summary>
        /// Compress 
        /// </summary>
        /// <returns></returns>
        public override byte[] Compress(byte[] Data) => Compressors.Gzip.CompressWithHeader(Data);
    }
}

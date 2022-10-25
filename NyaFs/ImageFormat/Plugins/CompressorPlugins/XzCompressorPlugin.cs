using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.CompressorPlugins
{
    class XzCompressorPlugin : Base.CompressorPlugin
    {

        public XzCompressorPlugin() : base("compr.xz", "xz", true, false) { }

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
        public override byte[] Decompress(byte[] Data) => Compressors.Xz.Decompress(Data);

    }
}

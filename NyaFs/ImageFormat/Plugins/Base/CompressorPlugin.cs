using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Plugins.Base
{
    public class CompressorPlugin : NyaPlugin
    {
        public readonly string CompressorName;

        private bool Write;
        private bool Read;

        public CompressorPlugin(string Name, string CompressorName, bool Read, bool Write) : base(Name, "compressor")
        {
            this.CompressorName = CompressorName;

            this.Read = Read;
            this.Write = Write;
        }

        /// <summary>
        /// Is image contain this filesystem
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public virtual bool DetectCompressor(byte[] Image) => false;

        /// <summary>
        /// Decompress
        /// </summary>
        /// <returns></returns>
        public virtual byte[] Decompress(byte[] Data) => null;

        /// <summary>
        /// Compress
        /// </summary>
        /// <returns></returns>
        public virtual byte[] Compress(byte[] Data) => null;
    }
}

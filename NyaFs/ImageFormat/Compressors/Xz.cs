using SharpCompress.Compressors.Xz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Compressors
{
    static class Xz
    {
        public static byte[] Decompress(byte[] Data)
        {
            using (MemoryStream input = new MemoryStream(Data))
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
    }
}

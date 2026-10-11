using NyaFs.ImageFormat.Plugins.Base;
using NyaFsBmu.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsBmu
{
    internal class NyaFsBmuCompositeImagePlugin : CompositeImagePlugin
    {
        public NyaFsBmuCompositeImagePlugin() : base("image.bmu", "bmu")
        {

        }

        /// <summary>
        /// Is image contain this image
        /// </summary>
        /// <param name="Image"></param>
        /// <returns></returns>
        public override bool DetectImage(byte[] Image)
        {
            return BmuImage.IsBmuImage(Image);
        }


    }
}

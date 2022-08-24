using NyaFs.ImageFormat.Elements.Dtb;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Elements.Kernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat
{
    public class BaseImageBlob
    {
        public virtual bool IsProvidedDTB => Dtb != null;
        public virtual bool IsProvidedKernel => Kernel != null;
        public virtual bool IsProvidedFs => Fs != null;

        protected DeviceTree  Dtb = null;
        protected Filesystem  Fs = null;
        protected LinuxKernel Kernel = null;

        public virtual void SetDevTree(int Index, DeviceTree Dtb) => this.Dtb = Dtb;
        public virtual void SetKernel(int Index, LinuxKernel Kernel) => this.Kernel = Kernel;
        public virtual void SetFilesystem(int Index, Filesystem Fs) => this.Fs = Fs;

        public virtual DeviceTree GetDevTree(int Index = 0) => Dtb;
        public virtual Filesystem GetFilesystem(int Index = 0) => Fs;
        public virtual LinuxKernel GetKernel(int Index = 0) => Kernel;
    }
}

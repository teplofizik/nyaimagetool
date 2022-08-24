using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Helper
{
    static class LogHelper
    {
        public static void KernelInfo(Elements.Kernel.LinuxKernel Kernel)
        {
            Log.Ok(1, "Kernel:");
            Log.Write(1, $"  Operating System: {FitHelper.GetOperatingSystem(Kernel.Info.OperatingSystem)}");
            Log.Write(1, $"      Architecture: {FitHelper.GetCPUArchitecture(Kernel.Info.Architecture)}");
            //Log.Write(1, $"       Compression: {FitHelper.GetCompression(Kernel.Info.Compression)}");
            Log.Write(1, $"              Type: {FitHelper.GetType(Kernel.Info.Type)}");
            Log.Write(1, $"      Load address: {Kernel.Info.DataLoadAddress:x08}");
            Log.Write(1, $"     Entry address: {Kernel.Info.EntryPointAddress:x08}");
        }

        public static void RamfsInfo(Elements.Fs.Filesystem Fs, string FsType)
        {
            Log.Ok(1, "Filesystem:");
            Log.Write(1, $"  Operating System: {FitHelper.GetOperatingSystem(Fs.Info.OperatingSystem)}");
            Log.Write(1, $"      Architecture: {FitHelper.GetCPUArchitecture(Fs.Info.Architecture)}");
            //Log.Write(1, $"       Compression: {FitHelper.GetCompression(Kernel.Info.Compression)}");
            Log.Write(1, $"              Type: {FitHelper.GetType(Fs.Info.Type)}");
            Log.Write(1, $"        Filesystem: {FsType}");

        }
        public static void DevtreeInfo(Elements.Dtb.DeviceTree Dtb)
        {
            Log.Ok(1, "Device tree:");
            Log.Write(1, $"      Architecture: {FitHelper.GetCPUArchitecture(Dtb.Info.Architecture)}");
            //Log.Write(1, $"       Compression: {FitHelper.GetCompression(Kernel.Info.Compression)}");
            Log.Write(1, $"              Type: {FitHelper.GetType(Dtb.Info.Type)}");
        }
    }
}

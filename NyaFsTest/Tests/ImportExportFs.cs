using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTest.Tests
{
    static class ImportExportFs
    {
        public static void TestImportNative()
        {
            var Dir = "example\\";

            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.NativeReader(Dir, 0, 0, 0x744, 0x755);
            Importer.ReadToFs(Fs);

            Fs.Dump();
        }

        public static void TestImportCpio()
        {
            var Fn = "example.cpio";

            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.CpioFsReader(Fn);
            Importer.ReadToFs(Fs);

            Fs.Dump();
        }

        public static void TestImportRamFsExt4()
        {
            var Fn = "legacy.bin";

            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.LegacyReader(Fn);
            Importer.ReadToFs(Fs);

            Fs.Dump();
        }

        public static void TestImportRamFsCpio()
        {
            var Fn = "legacy.bin";

            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.LegacyReader(Fn);
            Importer.ReadToFs(Fs);

            Fs.Dump();
        }

        public static void TestImportRamFsCpioExportNative()
        {
            var Fn = "legacy.bin";
            var Dst = "extracted\\";

            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.LegacyReader(Fn);
            Importer.ReadToFs(Fs);

            Fs.Dump();

            var Exporter = new NyaFs.ImageFormat.Elements.Fs.Writer.NativeWriter(Dst);
            Exporter.WriteFs(Fs);
        }
        public static void TestImportRamFsCpioExportCpio()
        {
            var Fn = "initramfs.bin";
            var Dst = "extracted.cpio";

            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.LegacyReader(Fn);
            Importer.ReadToFs(Fs);

            Fs.Dump();

            var Exporter = new NyaFs.ImageFormat.Elements.Fs.Writer.CpioFsWriter(Dst);
            Exporter.WriteFs(Fs);
        }
        public static void TestImportRamFsCpioExportGzCpio()
        {
            var Fn = "legacy.bin";
            var Dst = "extracted.gz";

            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.LegacyReader(Fn);
            Importer.ReadToFs(Fs);

            Fs.Dump();

            var Exporter = new NyaFs.ImageFormat.Elements.Fs.Writer.ArchiveCpioWriter(Dst, NyaFs.ImageFormat.Types.CompressionType.IH_COMP_GZIP);
            Exporter.WriteFs(Fs);
        }

        public static void TestImportRamFsCpioExportRamFsCpio()
        {
            var Fn = "legacy.bin";
            var Dst = "legacy.bin.saved";

            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();
            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.LegacyReader(Fn);
            Importer.ReadToFs(Fs);

            Fs.Dump();

            var Exporter = new NyaFs.ImageFormat.Elements.Fs.Writer.LegacyWriter(Dst);
            Exporter.WriteFs(Fs);
        }

        public static void TestImportFit()
        {
            var Fn = "test.fit";
            var Fs = new NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem();

            var Importer = new NyaFs.ImageFormat.Elements.Fs.Reader.FitReader(Fn, null);
            Importer.ReadToFs(Fs);

            Fs.Dump();
        }
    }
}

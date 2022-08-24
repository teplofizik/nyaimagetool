using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsTest
{
    static class ImportExportDt
    {
        static void LoadSaveFdt()
        {
            var fdt = new NyaFs.FlattenedDeviceTree.Reader.FDTReader("example.dtb").Read();

            if (fdt != null)
            {
                Console.WriteLine("FDT ok");

                var data = new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(fdt);
                System.IO.File.WriteAllBytes("example-saved.dtb", data.GetBinary());

                var readout = new NyaFs.FlattenedDeviceTree.Reader.FDTReader("example-saved.dtb").Read();

            }
        }

        static void LoadSaveFit()
        {
            var fdt = new NyaFs.FlattenedDeviceTree.Reader.FDTReader("example.fit").Read();

            if (fdt != null)
            {
                Console.WriteLine("FIT ok");

                var data = new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(fdt);
                System.IO.File.WriteAllBytes("example-saved.fit", data.GetBinary());

                var readout = new NyaFs.FlattenedDeviceTree.Reader.FDTReader("amlogic-a113d-saved.fit").Read();
            }
        }

        static void LoadFdt()
        {
            var fdt = new NyaFs.FlattenedDeviceTree.Reader.FDTReader("example.dtb").Read();

            if (fdt != null)
            {
                Console.WriteLine("FIT ok");
            }
        }

        static void LoadFit()
        {
            var fit = new NyaFs.FlattenedDeviceTree.Reader.FDTReader("example.fit").Read();

            if (fit != null)
            {
                Console.WriteLine("FIT ok");
            }
        }
    }
}

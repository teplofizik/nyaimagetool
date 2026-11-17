using System;
using System.IO;
using System.Linq;
using System.Text;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Composite;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Xunit;

namespace NyaFsTests
{
    public class DiskPlanTests
    {
        private static ImageProcessor Processor() =>
            new ImageProcessor(new ScriptParser(new ScriptBaseAll()), new string[0]);

        private static void Run(ImageProcessor Proc, params string[] Lines) =>
            Proc.Process(new ScriptParser(new ScriptBaseAll()).ParseScript("t", Lines));

        [Fact]
        public void Plan_Mbr_Builds_Table_And_Payloads()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "disk-plan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Dir);

            var Layout = Path.Combine(Dir, "mbr.yaml");
            var Boot = Path.Combine(Dir, "boot.bin");
            var Root = Path.Combine(Dir, "root.bin");
            var Out = Path.Combine(Dir, "disk.img");

            try
            {
                File.WriteAllBytes(Boot, Enumerable.Repeat((byte)0xA1, 4096).ToArray());
                File.WriteAllBytes(Root, Enumerable.Repeat((byte)0xB2, 8192).ToArray());

                File.WriteAllText(Layout,
                    "schema: 1\nformat: disk\n" +
                    "options: { table: mbr, align: 1048576 }\n" +
                    "children:\n" +
                    "  - { name: boot, offset: 1048576, size: 2097152, kind: raw, mbr-type: 0x0c, content: { file: \"boot.bin\" } }\n" +
                    "  - { name: root, offset: 4194304, size: 4194304, kind: raw, mbr-type: 0x83, content: { file: \"root.bin\" } }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc,
                    "mkimage disk \"" + Layout + "\"",
                    "store \"" + Out + "\" all disk");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal(0x55, Data[510]);
                Assert.Equal(0xAA, Data[511]);

                Assert.True(DiskImage.TryParse(Data, out var Disk));
                Assert.False(Disk.IsGpt);
                Assert.Equal(2, Disk.Partitions.Count);

                var BootPart = Disk.Partitions[0];
                Assert.Equal(0x0C, BootPart.MbrType);
                Assert.Equal(1048576 / 512, BootPart.StartLba);
                Assert.Equal(2097152 / 512, BootPart.SectorCount);

                var RootPart = Disk.Partitions[1];
                Assert.Equal(0x83, RootPart.MbrType);
                Assert.Equal(4194304 / 512, RootPart.StartLba);
                Assert.Equal(4194304 / 512, RootPart.SectorCount);

                Assert.Equal(0xA1, Data[1048576]);
                Assert.Equal(0xB2, Data[4194304]);
                Assert.Equal(0x00, Data[2097152]);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        [Fact]
        public void Plan_Gpt_Builds_Headers_Crc_And_Names()
        {
            var Dir = Path.Combine(Path.GetTempPath(), "disk-plan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Dir);

            var Layout = Path.Combine(Dir, "gpt.yaml");
            var Boot = Path.Combine(Dir, "boot.bin");
            var Root = Path.Combine(Dir, "root.bin");
            var Out = Path.Combine(Dir, "disk.img");

            try
            {
                File.WriteAllBytes(Boot, Enumerable.Repeat((byte)0xC3, 4096).ToArray());
                File.WriteAllBytes(Root, Enumerable.Repeat((byte)0xD4, 8192).ToArray());

                File.WriteAllText(Layout,
                    "schema: 1\nformat: disk\n" +
                    "options: { table: gpt, align: 1048576, seed: test }\n" +
                    "children:\n" +
                    "  - { name: boot, offset: 1048576, size: 4194304, kind: raw, content: { file: \"boot.bin\" } }\n" +
                    "  - { name: root, offset: 8388608, size: 4194304, kind: raw, content: { file: \"root.bin\" } }\n",
                    new UTF8Encoding(false));

                var Proc = Processor();
                Run(Proc,
                    "mkimage disk \"" + Layout + "\"",
                    "store \"" + Out + "\" all disk");

                var Data = File.ReadAllBytes(Out);
                Assert.Equal(0x55, Data[510]);
                Assert.Equal(0xAA, Data[511]);
                Assert.Equal((byte)'E', Data[512]);
                Assert.Equal((byte)'F', Data[513]);

                Assert.True(DiskImage.TryParse(Data, out var Disk));
                Assert.True(Disk.IsGpt);
                Assert.Equal(2, Disk.Partitions.Count);
                Assert.Equal("boot", Disk.Partitions[0].Name);
                Assert.Equal("root", Disk.Partitions[1].Name);
                Assert.Equal(1048576 / 512, Disk.Partitions[0].StartLba);
                Assert.Equal(4194304 / 512, Disk.Partitions[0].SectorCount);
                Assert.Equal(8388608 / 512, Disk.Partitions[1].StartLba);
                Assert.Equal(DiskImageBuilder.LinuxDataGuid,
                    Guid.Parse(Disk.Partitions[0].TypeGuid).ToString().ToUpperInvariant());

                var Header = new byte[92];
                Buffer.BlockCopy(Data, 512, Header, 0, 92);
                var StoredCrc = ReadU32(Header, 16);
                WriteU32(Header, 16, 0);
                Assert.Equal(StoredCrc, Crc32(Header, 0, 92));
                Assert.Equal(1UL, ReadU64(Header, 24));
                Assert.Equal((ulong)(Data.Length / 512 - 1), ReadU64(Header, 32));

                var EntriesLba = ReadU64(Header, 72);
                var Entries = new byte[128 * 128];
                Buffer.BlockCopy(Data, (int)(EntriesLba * 512), Entries, 0, Entries.Length);
                Assert.Equal(ReadU32(Header, 88), Crc32(Entries, 0, Entries.Length));

                var BackupHeader = new byte[92];
                Buffer.BlockCopy(Data, Data.Length - 512, BackupHeader, 0, 92);
                Assert.Equal((byte)'E', BackupHeader[0]);
                Assert.Equal((ulong)(Data.Length / 512 - 1), ReadU64(BackupHeader, 24));
                Assert.Equal(1UL, ReadU64(BackupHeader, 32));

                Assert.Equal(0xC3, Data[1048576]);
                Assert.Equal(0xD4, Data[8388608]);
            }
            finally
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }
        }

        private static uint ReadU32(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));

        private static ulong ReadU64(byte[] Data, int Offset) =>
            ReadU32(Data, Offset) | ((ulong)ReadU32(Data, Offset + 4) << 32);

        private static void WriteU32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }

        private static uint Crc32(byte[] Data, int Offset, int Length)
        {
            var Crc = 0xFFFFFFFFu;
            for (int I = 0; I < Length; I++)
            {
                Crc ^= Data[Offset + I];
                for (int K = 0; K < 8; K++)
                    Crc = ((Crc & 1) != 0) ? (0xEDB88320u ^ (Crc >> 1)) : (Crc >> 1);
            }
            return Crc ^ 0xFFFFFFFF;
        }
    }
}

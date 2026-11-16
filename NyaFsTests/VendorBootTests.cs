using System;
using System.Text;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Types.Android;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using Xunit;

namespace NyaFsTests
{
    public class VendorBootTests
    {
        private const int Page = 0x1000;

        private static long Align(long Value, long Alignment) =>
            ((Value + Alignment - 1) / Alignment) * Alignment;

        private static void U32(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }

        private static void U64(byte[] Data, int Offset, ulong Value)
        {
            U32(Data, Offset, (uint)Value);
            U32(Data, Offset + 4, (uint)(Value >> 32));
        }

        private static void Str(byte[] Data, int Offset, int Size, string Value)
        {
            var Bytes = Encoding.ASCII.GetBytes(Value ?? "");
            Buffer.BlockCopy(Bytes, 0, Data, Offset, Math.Min(Size, Bytes.Length));
        }

        private static void Header(byte[] Data, uint Version, uint HeaderSize)
        {
            Str(Data, 0, 8, "VNDRBOOT");
            U32(Data, 0x08, Version);
            U32(Data, 0x0C, Page);
            U32(Data, 0x10, 0x10008000);
            U32(Data, 0x14, 0x10000000);
            U32(Data, 0x81C, 0x10000100);
            Str(Data, 0x820, 16, "board");
            U32(Data, 0x830, HeaderSize);
            U64(Data, 0x838, 0x11F00000);
        }

        private static byte[] BuildV3(byte[] Ramdisk, byte[] Dtb)
        {
            var RamdiskOffset = Page;
            var DtbOffset = Align(RamdiskOffset + Ramdisk.Length, Page);
            var Total = Align(DtbOffset + Dtb.Length, Page);
            var Data = new byte[Total];
            Header(Data, 3, 0x840);
            U32(Data, 0x18, (uint)Ramdisk.Length);
            U32(Data, 0x834, (uint)Dtb.Length);
            Buffer.BlockCopy(Ramdisk, 0, Data, RamdiskOffset, Ramdisk.Length);
            Buffer.BlockCopy(Dtb, 0, Data, (int)DtbOffset, Dtb.Length);
            return Data;
        }

        private static byte[] BuildV4(byte[][] Fragments, byte[] Dtb, byte[] Bootconfig)
        {
            var Region = new byte[0];
            var Sizes = new int[Fragments.Length];
            var Offsets = new int[Fragments.Length];
            var Position = 0;
            for (var I = 0; I < Fragments.Length; I++)
            {
                Sizes[I] = Fragments[I].Length;
                Offsets[I] = Position;
                Position += Fragments[I].Length;
            }
            Region = new byte[Position];
            for (var I = 0; I < Fragments.Length; I++)
                Buffer.BlockCopy(Fragments[I], 0, Region, Offsets[I], Fragments[I].Length);

            var EntrySize = VendorBootImage.TableEntryV4Size;
            var Table = new byte[EntrySize * Fragments.Length];
            for (var I = 0; I < Fragments.Length; I++)
            {
                var O = I * EntrySize;
                U32(Table, O + 0, (uint)Sizes[I]);
                U32(Table, O + 4, (uint)Offsets[I]);
                U32(Table, O + 8, (uint)(I + 1));
                Str(Table, O + 12, 32, "fragment" + I);
                U32(Table, O + 44, (uint)(100 + I));
            }

            var RamdiskOffset = Page;
            var DtbOffset = Align(RamdiskOffset + Region.Length, Page);
            var TableOffset = Align(DtbOffset + Dtb.Length, Page);
            var BootconfigOffset = Align(TableOffset + Table.Length, Page);
            var Total = Align(BootconfigOffset + Bootconfig.Length, Page);
            var Data = new byte[Total];
            Header(Data, 4, 0x850);
            U32(Data, 0x18, (uint)Region.Length);
            U32(Data, 0x834, (uint)Dtb.Length);
            U32(Data, 0x840, (uint)Table.Length);
            U32(Data, 0x844, (uint)Fragments.Length);
            U32(Data, 0x848, (uint)EntrySize);
            U32(Data, 0x84C, (uint)Bootconfig.Length);

            Buffer.BlockCopy(Region, 0, Data, RamdiskOffset, Region.Length);
            Buffer.BlockCopy(Dtb, 0, Data, (int)DtbOffset, Dtb.Length);
            Buffer.BlockCopy(Table, 0, Data, (int)TableOffset, Table.Length);
            Buffer.BlockCopy(Bootconfig, 0, Data, (int)BootconfigOffset, Bootconfig.Length);
            return Data;
        }

        private static ScriptPlugins Plugins()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            return Proc.Plugins;
        }

        [Fact]
        public void VendorBootV3_Is_Detected_And_Loaded()
        {
            var Data = BuildV3(Encoding.ASCII.GetBytes("ramdisk-v3"), new byte[] { 1, 2, 3, 4 });

            var Composite = Plugins().DetectComposite(new ImageData(Data));
            Assert.NotNull(Composite);
            Assert.Equal("vendorboot", Composite.Format);

            var Images = Composite.GetImages(new ImageData(Data));
            Assert.Contains(Images, I => I.Name == "vendor_ramdisk");
            Assert.Contains(Images, I => I.Name == "dtb");

            var Blob = new BaseImageBlob();
            Assert.True(new ImagePipeline(Plugins(), Blob).Load(Data));
            Assert.NotNull(Blob.BuildPlan);
            Assert.NotNull(Blob.BuildPlan.Find("vendor_ramdisk"));
            Assert.NotNull(Blob.BuildPlan.Find("dtb"));
        }

        [Fact]
        public void VendorBootV4_Is_Detected_And_Loaded()
        {
            var Data = BuildV4(
                new[] { Encoding.ASCII.GetBytes("fragment-a"), Encoding.ASCII.GetBytes("fragment-b") },
                new byte[] { 1, 2, 3, 4 },
                Encoding.ASCII.GetBytes("androidboot.test=1\n"));

            var Boot = new VendorBootImage(Data);
            Assert.True(Boot.Correct);
            Assert.Equal(4u, Boot.HeaderVersion);
            Assert.Equal(2, Boot.Entries.Count);
            Assert.Equal("fragment0", Boot.Entries[0].Name);
            Assert.Equal(100u, Boot.Entries[0].BoardId[0]);

            var Composite = Plugins().DetectComposite(new ImageData(Data));
            Assert.NotNull(Composite);
            var Images = Composite.GetImages(new ImageData(Data));
            Assert.Contains(Images, I => I.Name == "vendor_ramdisk_0");
            Assert.Contains(Images, I => I.Name == "vendor_ramdisk_1");
            Assert.Contains(Images, I => I.Name == "dtb");
            Assert.Contains(Images, I => I.Name == "bootconfig");

            var Blob = new BaseImageBlob();
            Assert.True(new ImagePipeline(Plugins(), Blob).Load(Data));
            Assert.NotNull(Blob.BuildPlan.Find("vendor_ramdisk_0"));
            Assert.NotNull(Blob.BuildPlan.Find("vendor_ramdisk_1"));
            Assert.NotNull(Blob.BuildPlan.Find("bootconfig"));
        }

        [Fact]
        public void VendorBootV4_Rebuilds_Changed_Fragment()
        {
            var Data = BuildV4(
                new[] { Encoding.ASCII.GetBytes("fragment-a"), Encoding.ASCII.GetBytes("fragment-b") },
                new byte[] { 1, 2, 3, 4 },
                Encoding.ASCII.GetBytes("androidboot.test=1\n"));

            var Blob = new BaseImageBlob();
            Assert.True(new ImagePipeline(Plugins(), Blob).Load(Data));

            var Child = Blob.BuildPlan.Find("vendor_ramdisk_0");
            Assert.NotNull(Child);
            var NewFragment = Encoding.ASCII.GetBytes("changed-fragment");
            Child.Raw = NewFragment;
            Child.Dirty = true;
            Blob.BuildPlan.Root.Dirty = true;

            var Plugin = new NyaFs.ImageFormat.Plugins.ImagePlugins.CompositeImagePlugins.VendorBootPlugin();
            var Built = Plugin.Build(Blob, out var Error);
            Assert.Null(Error);
            Assert.NotNull(Built);

            var Boot = new VendorBootImage(Built);
            Assert.True(Boot.Correct);
            Assert.Equal(2, Boot.Entries.Count);
            Assert.Equal((uint)NewFragment.Length, Boot.Entries[0].Size);
            Assert.Equal(NewFragment, Boot.GetEntryData(Boot.Entries[0]));
            Assert.Equal(Encoding.ASCII.GetBytes("fragment-b"), Boot.GetEntryData(Boot.Entries[1]));
            Assert.Equal("fragment0", Boot.Entries[0].Name);
            Assert.Equal(100u, Boot.Entries[0].BoardId[0]);
            Assert.Equal("androidboot.test=1\n", Encoding.ASCII.GetString(Boot.Bootconfig));
        }

        [Fact]
        public void VendorBootV3_Rebuilds_Changed_Ramdisk()
        {
            var Data = BuildV3(Encoding.ASCII.GetBytes("ramdisk-v3"), new byte[] { 1, 2, 3, 4 });

            var Blob = new BaseImageBlob();
            Assert.True(new ImagePipeline(Plugins(), Blob).Load(Data));

            var Child = Blob.BuildPlan.Find("vendor_ramdisk");
            Child.Raw = Encoding.ASCII.GetBytes("new-ramdisk");
            Child.Dirty = true;
            Blob.BuildPlan.Root.Dirty = true;

            var Plugin = new NyaFs.ImageFormat.Plugins.ImagePlugins.CompositeImagePlugins.VendorBootPlugin();
            var Built = Plugin.Build(Blob, out var Error);
            Assert.Null(Error);

            var Boot = new VendorBootImage(Built);
            Assert.True(Boot.Correct);
            Assert.Equal(3u, Boot.HeaderVersion);
            Assert.Equal(Encoding.ASCII.GetBytes("new-ramdisk"), Boot.VendorRamdisk);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, Boot.Dtb);
        }
    }
}

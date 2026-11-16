using System;
using System.IO;
using System.Linq;
using NyaFs.ImageFormat;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFsNand;
using NyaFsNand.Types;
using Xunit;

namespace NyaFsTests
{
    public class NandTests
    {
        private const int Page = 2048;
        private const int Oob = 64;
        private const int BlockSize = 0x20000;

        private static byte[] Fixture(string Name) =>
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", Name));

        /// <summary>Wrap a logical image into a raw NAND dump with an all-FF spare area.</summary>
        private static byte[] Wrap(byte[] Logical, int PageSize, int OobSize)
        {
            Assert.Equal(0, Logical.Length % PageSize);

            var Pages = Logical.Length / PageSize;
            var Result = new byte[Pages * (PageSize + OobSize)];

            for (int I = 0; I < Pages; I++)
            {
                Array.Copy(Logical, I * PageSize, Result, I * (PageSize + OobSize), PageSize);

                for (int K = 0; K < OobSize; K++)
                    Result[I * (PageSize + OobSize) + PageSize + K] = 0xFF;
            }

            return Result;
        }

        [Fact]
        public void Nand_Detects_Geometry_And_Strips_Oob()
        {
            var Ubi = Fixture("ubifs_sample.ubi");
            Assert.Equal(0, Ubi.Length % BlockSize);

            var Raw = Wrap(Ubi, Page, Oob);
            var Plugin = new NyaFsNandCompositeImagePlugin();
            var Image = new ImageData(Raw, "dump.bin");

            Assert.True(Plugin.DetectImage(Image));

            var Options = Plugin.ReadOptions(Image).ToDictionary(X => X.Key, X => X.Value);
            Assert.Equal(Page.ToString(), Options["page"]);
            Assert.Equal(Oob.ToString(), Options["oob"]);
            Assert.Equal(BlockSize.ToString(), Options["block"]);

            var Images = Plugin.GetImages(Image);
            Assert.Single(Images);

            var Region = Images[0];
            Assert.StartsWith("ubi@", Region.Name);
            Assert.Equal(Ubi.Length, Region.Size);
            Assert.Equal(Ubi, Region.Content);
        }

        [Fact]
        public void Nand_Is_Not_Detected_For_A_Plain_Ubi_Image()
        {
            var Ubi = Fixture("ubifs_sample.ubi");
            var Plugin = new NyaFsNandCompositeImagePlugin();

            Assert.False(Plugin.DetectImage(new ImageData(Ubi, "sample.ubi")));
        }

        [Fact]
        public void Nand_Explicit_Load_Accepts_A_Data_Only_Dump()
        {
            var Ubi = Fixture("ubifs_sample.ubi");
            var Nand = NandImage.TryParse(Ubi, 0, Ubi.Length, "sample.ubin", true);

            Assert.NotNull(Nand);
            Assert.Equal(Page, Nand.Geometry.PageSize);
            Assert.Equal(0, Nand.Geometry.OobSize);
            Assert.Equal(BlockSize, Nand.Geometry.BlockSize);
            Assert.Single(Nand.Regions);
        }

        [Fact]
        public void Nand_Inspector_Reads_The_Filesystem_Inside()
        {
            var Ubi = Fixture("ubifs_sample.ubi");
            var Raw = Wrap(Ubi, Page, Oob);

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.Plugins.Load(new NyaFsNandCompositeImagePlugin());

            var Root = new ImageInspector(Proc.Plugins).Inspect("dump.bin", Raw, "dump.bin");

            Assert.Equal("nand", Root.Format);
            Assert.Single(Root.Children);
            Assert.Equal(ImageKind.Filesystem, Root.Children[0].Kind);
            Assert.Equal("ubifs", Root.Children[0].Format);
        }

        [Fact]
        public void Nand_Detects_Logical_Mtd_Image_Without_Oob()
        {
            var Ubi = Fixture("ubifs_sample.ubi");

            var Raw = new byte[Ubi.Length + 3 * BlockSize];
            for (var I = 0; I < 3 * BlockSize; I++)
                Raw[I] = 0xA5;
            Array.Copy(Ubi, 0, Raw, 3 * BlockSize, Ubi.Length);

            var Plugin = new NyaFsNandCompositeImagePlugin();
            var Image = new ImageData(Raw, "flash.bin");
            Assert.True(Plugin.DetectImage(Image));

            var Nand = NandImage.TryParse(Raw, 0, Raw.Length, "flash.bin");
            Assert.NotNull(Nand);
            Assert.True(Nand.Logical);
            Assert.Equal(BlockSize, Nand.Geometry.BlockSize);
            Assert.Equal(2, Nand.Regions.Length);

            var Region = Nand.Regions.Single(R => R.Kind == "ubi");
            Assert.Equal(3 * BlockSize, Region.Offset);
            Assert.Equal(Ubi, Nand.ReadLogical(Region.Offset, Region.Size));

            Assert.Equal(-1, Nand.BadBlocks);
        }

        [Fact]
        public void Nand_Ubi_Info_Reports_Volumes()
        {
            var Ubi = Fixture("ubifs_sample.ubi");
            var Raw = Wrap(Ubi, Page, Oob);

            var Nand = NandImage.TryParse(Raw, 0, Raw.Length, "dump.bin");
            Assert.NotNull(Nand);
            Assert.Equal(0, Nand.BadBlocks);

            var Device = Assert.Single(Nand.ReadUbiDevices());
            Assert.Equal(21, Device.Pebs);
            Assert.Equal(2, Device.LayoutPebs);
            Assert.Equal(0, Device.BadEcPebs);
            Assert.Equal(0, Device.ErasedPebs);

            var Volume = Assert.Single(Device.Volumes, V => V.Name == "rootfs");
            Assert.Equal(19u, Volume.ReservedPebs);
            Assert.Equal(19, Volume.MappedLebs);
            Assert.Equal("dynamic", Volume.TypeName);

            Assert.Equal(19, Volume.Lebs.Count);
            Assert.Equal(0u, Volume.Lebs[0].LNum);
            Assert.Equal(Volume.Lebs.OrderBy(L => L.LNum).Select(L => L.LNum), Volume.Lebs.Select(L => L.LNum));
            Assert.Same(Volume, Device.FindVolume("rootfs"));
            Assert.Same(Volume, Device.FindVolume("0"));
        }

        [Fact]
        public void Nand_Blocks_Table_Reports_State()
        {
            var Ubi = Fixture("ubifs_sample.ubi");
            var Raw = Wrap(Ubi, Page, Oob);

            var Nand = NandImage.TryParse(Raw, 0, Raw.Length, "dump.bin");
            Assert.NotNull(Nand);

            var Blocks = Nand.ReadBlocks();
            Assert.Equal(21, Blocks.Count);
            Assert.All(Blocks, B => Assert.True(B.Ubi));
            Assert.Equal(2, Blocks.Count(B => B.Layout));
            Assert.Equal(19, Blocks.Count(B => B.HasVid && !B.Layout));

            Assert.Equal(0x21000L, Blocks[1].RawOffset);
            Assert.Equal(0x20000L, Blocks[1].LogicalOffset);
        }

        [Fact]
        public void Nand_Blocks_Table_Marks_Data_Blocks()
        {
            var Ubi = Fixture("ubifs_sample.ubi");

            var Raw = new byte[Ubi.Length + 2 * BlockSize];
            for (var I = 0; I < 2 * BlockSize; I++)
                Raw[I] = 0xA5;
            Array.Copy(Ubi, 0, Raw, 2 * BlockSize, Ubi.Length);

            var Nand = NandImage.TryParse(Raw, 0, Raw.Length, "flash.nand");
            Assert.NotNull(Nand);

            var Blocks = Nand.ReadBlocks();
            Assert.Equal(23, Blocks.Count);
            Assert.All(Blocks.Take(2), B => Assert.True(B.Used && !B.Ubi && B.Container == null));
            Assert.All(Blocks.Skip(2), B => Assert.True(B.Ubi));
        }
    }
}

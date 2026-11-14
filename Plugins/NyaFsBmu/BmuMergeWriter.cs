using NyaFs;
using NyaFs.ImageFormat;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsBmu
{
    /// <summary>
    /// Rebuilds a merged Bitmain BMU (magic ABABABAB). Item metadata and the
    /// source header are preserved; dirty inner single BMUs are re-signed and
    /// their offsets/sizes in the item table are refreshed.
    /// </summary>
    public static class BmuMergeWriter
    {
        public static bool IsMerge(byte[] Source) =>
            (Source != null) && (Source.Length >= 4) &&
            (Source[0] == 0xAB) && (Source[1] == 0xAB) &&
            (Source[2] == 0xAB) && (Source[3] == 0xAB);

        public static byte[] Build(BaseImageBlob Blob, byte[] Source, out string Error)
        {
            Error = null;
            try
            {
                if (!IsDirty(Blob.BuildPlan.Root))
                    return Source;

                var HeaderSize = (int)ReadU32LE(Source, 0x08);
                var Count = (int)ReadU32LE(Source, 0x0C);
                var ItemSize = (int)ReadU32LE(Source, 0x10);
                var DataOffset = (int)ReadU32LE(Source, 0x14);
                if ((HeaderSize < 36) || (Count < 0) || (ItemSize < 172) ||
                    (HeaderSize + Count * ItemSize > Source.Length) || (DataOffset < HeaderSize))
                {
                    Error = "Invalid merged BMU header";
                    return null;
                }

                var Items = new List<MergeItem>();
                for (var I = 0; I < Count; I++)
                {
                    var Base = HeaderSize + I * ItemSize;
                    var Filename = ReadAnsi(Source, Base + 4, Source[Base + 0], 0x60);
                    var Chip = ReadAnsi(Source, Base + 0x44, Source[Base + 1], 0x20);
                    var Hardware = ReadAnsi(Source, Base + 0x64, Source[Base + 2], 0x20);
                    var Model = ReadAnsi(Source, Base + 0x84, Source[Base + 3], 0x20);
                    var Offset = (int)ReadU32LE(Source, Base + 0xA4);
                    var Size = (int)ReadU32LE(Source, Base + 0xA8);
                    if ((Offset < 0) || (Size < 0) || (Offset + (long)Size > Source.Length))
                    {
                        Error = "Invalid merged BMU item";
                        return null;
                    }

                    var Segments = new List<string>();
                    if (Model.Length > 0) Segments.Add(Model);
                    if (Hardware.Length > 0) Segments.Add(Hardware);
                    if (Chip.Length > 0) Segments.Add(Chip);
                    Segments.Add(Filename);
                    var Name = string.Join("/", Segments);

                    var Child = FindChild(Blob.BuildPlan.Root, Name);
                    var Data = ((Child != null) && IsDirty(Child))
                        ? BuildInner(Blob, Child, Name, out Error)
                        : Slice(Source, Offset, Size);
                    if (Data == null)
                        return null;

                    Items.Add(new MergeItem
                    {
                        Name = Name,
                        Data = Data,
                        ItemOffset = Base
                    });
                }

                var Total = (long)DataOffset;
                foreach (var Item in Items)
                    Total += Item.Data.Length;
                if ((Total <= 0) || (Total > Int32.MaxValue))
                {
                    Error = "Merged BMU is too large";
                    return null;
                }

                var Result = new byte[(int)Total];
                Buffer.BlockCopy(Source, 0, Result, 0, DataOffset);

                var DataPosition = DataOffset;
                foreach (var Item in Items)
                {
                    SetU32LE(Result, Item.ItemOffset + 0xA4, (uint)DataPosition);
                    SetU32LE(Result, Item.ItemOffset + 0xA8, (uint)Item.Data.Length);
                    Buffer.BlockCopy(Item.Data, 0, Result, DataPosition, Item.Data.Length);
                    DataPosition += Item.Data.Length;
                }

                return Result;
            }
            catch (Exception E)
            {
                Error = E.Message;
                return null;
            }
        }

        private static byte[] BuildInner(BaseImageBlob Parent, ImageBuildNode Child, string Name, out string Error)
        {
            Error = null;
            var Temp = new BaseImageBlob();
            Temp.BuildPlan = new ImageBuildPlan(Child);
            foreach (var Option in new[] { "sign-key", "root-key" })
            {
                var Value = Parent.GetContainerOption("bmu", Option, null);
                if (!String.IsNullOrEmpty(Value))
                    Temp.SetContainerOption("bmu", Option, Value);
            }

            var Data = BmuWriter.Build(Temp, out Error);
            if (Data == null)
                Error = $"Cannot rebuild BMU item '{Name}': {Error}";
            return Data;
        }

        private static ImageBuildNode FindChild(ImageBuildNode Node, string Name)
        {
            if (Node == null)
                return null;
            foreach (var Child in Node.Children)
            {
                if (String.Equals(Child.Name, Name, StringComparison.OrdinalIgnoreCase))
                    return Child;
                var Res = FindChild(Child, Name);
                if (Res != null)
                    return Res;
            }
            return null;
        }

        private static bool IsDirty(ImageBuildNode Node)
        {
            if (Node == null)
                return false;
            if (Node.Dirty)
                return true;
            foreach (var Child in Node.Children)
                if (IsDirty(Child))
                    return true;
            return false;
        }

        private static string ReadAnsi(byte[] Data, int Offset, int Length, int FieldSize)
        {
            if ((Length <= 0) || (Length > FieldSize) || (Offset < 0) || (Offset + Length > Data.Length))
                return "";
            return Encoding.ASCII.GetString(Data, Offset, Length);
        }

        private static byte[] Slice(byte[] Data, int Offset, int Size)
        {
            var Result = new byte[Size];
            Buffer.BlockCopy(Data, Offset, Result, 0, Size);
            return Result;
        }

        private static uint ReadU32LE(byte[] Data, int Offset) =>
            (uint)(Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24));

        private static void SetU32LE(byte[] Data, int Offset, uint Value)
        {
            Data[Offset] = (byte)Value;
            Data[Offset + 1] = (byte)(Value >> 8);
            Data[Offset + 2] = (byte)(Value >> 16);
            Data[Offset + 3] = (byte)(Value >> 24);
        }

        private sealed class MergeItem
        {
            public string Name;
            public byte[] Data;
            public int ItemOffset;
        }
    }
}

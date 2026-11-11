using NyaFs.FlattenedDeviceTree.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NyaFsFit
{
    /// <summary>
    /// Export a FIT image as U-Boot .its source with sidecar binary blobs.
    /// Image data properties are emitted as /incbin/ references, not as text hex.
    /// </summary>
    public class FitSourceExporter
    {
        public bool Export(string Filename, string OutputDir, out string Error)
        {
            Error = null;

            byte[] Raw;
            try
            {
                Raw = File.ReadAllBytes(Filename);
            }
            catch (Exception E)
            {
                Error = "Cannot read " + Filename + ": " + E.Message;
                return false;
            }

            var Reader = new NyaFs.FlattenedDeviceTree.Reader.FDTReader(Raw);
            if (!Reader.Correct)
            {
                Error = "Not a valid FIT/FDT image";
                return false;
            }

            var Fit = Reader.Read();
            if (!Fit.HasNode("images") || !Fit.HasNode("configurations"))
            {
                Error = "Not a FIT image (images/configurations missing)";
                return false;
            }

            try
            {
                Directory.CreateDirectory(OutputDir);
                var BlobDir = Path.Combine(OutputDir, "blobs");
                Directory.CreateDirectory(BlobDir);

                var Sb = new StringBuilder();
                Sb.Append("/dts-v1/;\n\n");
                WriteNode(Sb, Fit.Root, "", true, false, BlobDir);

                var ItsPath = Path.Combine(OutputDir, Path.GetFileNameWithoutExtension(Filename) + ".its");
                File.WriteAllText(ItsPath, Sb.ToString(), new UTF8Encoding(false));
                return true;
            }
            catch (Exception E)
            {
                Error = E.Message;
                return false;
            }
        }

        private static void WriteNode(StringBuilder Sb, Node Node, string Indent, bool IsRoot,
            bool IsImage, string BlobDir)
        {
            var Inner = Indent + "\t";
            Sb.Append(IsRoot ? "/ {\n" : $"{Indent}{Node.Name} {{\n");

            foreach (var Property in Node.Properties)
            {
                if (IsImage && (Property.Name == "data"))
                {
                    var BlobName = SafeBlobName(Node.Name) + ".bin";
                    var BlobPath = Path.Combine(BlobDir, BlobName);
                    File.WriteAllBytes(BlobPath, Property.Value ?? new byte[0]);
                    Sb.Append($"{Inner}data = /incbin/(\"./blobs/{BlobName}\");\n");
                }
                else
                    Sb.Append($"{Inner}{Property.Name} = {FormatValue(Property.Value)};\n");
            }

            foreach (var Child in Node.Nodes)
                WriteNode(Sb, Child, Inner, false, Node.Name == "images", BlobDir);

            Sb.Append(IsRoot ? "};\n" : $"{Indent}}};\n");
        }

        private static string SafeBlobName(string Name)
        {
            var Res = new StringBuilder();
            foreach (var C in Name)
                Res.Append(Char.IsLetterOrDigit(C) || (C == '-') || (C == '_') || (C == '.') ? C : '_');
            return (Res.Length > 0) ? Res.ToString() : "image";
        }

        private static string FormatValue(byte[] Value)
        {
            if ((Value == null) || (Value.Length == 0))
                return "";

            if (IsText(Value))
                return FormatStrings(Value);

            if ((Value.Length % 4) == 0)
            {
                var Cells = new List<string>();
                for (var I = 0; I < Value.Length; I += 4)
                {
                    var Cell = (uint)((Value[I] << 24) | (Value[I + 1] << 16) | (Value[I + 2] << 8) | Value[I + 3]);
                    Cells.Add("0x" + Cell.ToString("x", CultureInfo.InvariantCulture));
                }
                return "<" + String.Join(" ", Cells) + ">";
            }

            var Bytes = new StringBuilder("[");
            for (var I = 0; I < Value.Length; I++)
            {
                if (I > 0) Bytes.Append(' ');
                Bytes.Append(Value[I].ToString("x2", CultureInfo.InvariantCulture));
            }
            Bytes.Append(']');
            return Bytes.ToString();
        }

        private static bool IsText(byte[] Value)
        {
            if ((Value.Length == 0) || (Value[Value.Length - 1] != 0))
                return false;

            var Start = 0;
            for (var I = 0; I < Value.Length; I++)
            {
                if (Value[I] != 0)
                    continue;

                if (I > Start)
                {
                    for (var K = Start; K < I; K++)
                        if ((Value[K] < 0x20) || (Value[K] > 0x7E))
                            return false;
                }
                else if (I != Value.Length - 1)
                    return false;

                Start = I + 1;
            }

            return true;
        }

        private static string FormatStrings(byte[] Value)
        {
            var Parts = new List<string>();
            var Start = 0;
            for (var I = 0; I < Value.Length; I++)
            {
                if (Value[I] != 0)
                    continue;

                Parts.Add(Escape(Encoding.ASCII.GetString(Value, Start, I - Start)));
                Start = I + 1;
            }

            if ((Parts.Count > 0) && (Parts[Parts.Count - 1].Length == 0))
                Parts.RemoveAt(Parts.Count - 1);

            return String.Join(", ", Parts.ConvertAll(P => "\"" + P + "\""));
        }

        private static string Escape(string Value) =>
            Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

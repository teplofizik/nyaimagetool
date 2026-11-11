using NyaFs.FlattenedDeviceTree;
using NyaFs.FlattenedDeviceTree.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NyaFsFit
{
    /// <summary>
    /// Minimal U-Boot .its importer for the source form emitted by FitSourceExporter.
    /// Supports nodes, strings, cells, byte arrays and /incbin/ payloads.
    /// </summary>
    public class FitSourceImporter
    {
        public bool Import(string ItsFile, string OutFile, out string Error)
        {
            Error = null;
            try
            {
                var Parser = new DtsParser(File.ReadAllText(ItsFile), Path.GetDirectoryName(ItsFile));
                var Tree = Parser.Parse();
                if (!Tree.HasNode("images") || !Tree.HasNode("configurations"))
                {
                    Error = "Not a FIT .its (images/configurations missing)";
                    return false;
                }

                var Data = new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(Tree).GetBinary();
                File.WriteAllBytes(OutFile, Data);
                return true;
            }
            catch (Exception E)
            {
                Error = E.Message;
                return false;
            }
        }

        private class DtsParser
        {
            private readonly string Text;
            private readonly string BaseDir;
            private int Pos;

            public DtsParser(string Text, string BaseDir)
            {
                this.Text = Text;
                this.BaseDir = BaseDir;
            }

            public FlattenedDeviceTree Parse()
            {
                Skip();
                if (Peek('/'))
                {
                    var P = Pos;
                    ReadWord();
                    if (Word == "/dts-v1/")
                    {
                        Expect(';');
                    }
                    else
                        Pos = P;
                }

                Skip();
                while (StartsWith("/memreserve/"))
                {
                    ReadUntil(';');
                    Expect(';');
                    Skip();
                }

                Expect('/');
                var Root = new Node("");
                ParseBody(Root);
                var Tree = new FlattenedDeviceTree();
                Tree.Root = Root;
                return Tree;
            }

            private void ParseBody(Node Node)
            {
                Expect('{');
                while (true)
                {
                    Skip();
                    if (Peek('}'))
                    {
                        Pos++;
                        Expect(';');
                        return;
                    }
                    if (Pos >= Text.Length)
                        throw new Exception("Unexpected end of .its");

                    var Name = ReadWord();
                    if (Name.Length == 0)
                        throw new Exception("Expected node or property name at " + Pos);

                    Skip();
                    if (Peek('{'))
                    {
                        var Child = new Node(Name);
                        ParseBody(Child);
                        Node.Nodes.Add(Child);
                    }
                    else
                    {
                        var Property = new Property(Name, ParsePropertyValue());
                        Node.Properties.Add(Property);
                    }
                }
            }

            private byte[] ParsePropertyValue()
            {
                Skip();
                if (Peek(';'))
                {
                    Pos++;
                    return new byte[0];
                }

                Expect('=');
                Skip();

                if (Peek('"'))
                {
                    var Bytes = new List<byte>();
                    while (true)
                    {
                        Bytes.AddRange(ReadString());
                        Skip();
                        if (Peek(','))
                        {
                            Pos++;
                            Skip();
                            continue;
                        }
                        break;
                    }
                    Expect(';');
                    return Bytes.ToArray();
                }

                if (Peek('<'))
                {
                    Pos++;
                    var Bytes = new List<byte>();
                    while (true)
                    {
                        Skip();
                        if (Peek('>'))
                        {
                            Pos++;
                            break;
                        }
                        var Cell = (uint)ReadNumber();
                        Bytes.Add((byte)(Cell >> 24));
                        Bytes.Add((byte)(Cell >> 16));
                        Bytes.Add((byte)(Cell >> 8));
                        Bytes.Add((byte)Cell);
                    }
                    Expect(';');
                    return Bytes.ToArray();
                }

                if (Peek('['))
                {
                    Pos++;
                    var Bytes = new List<byte>();
                    while (true)
                    {
                        Skip();
                        if (Peek(']'))
                        {
                            Pos++;
                            break;
                        }
                        var Word = ReadHexByte();
                        Bytes.Add((byte)Word);
                    }
                    Expect(';');
                    return Bytes.ToArray();
                }

                if (StartsWith("/incbin/"))
                {
                    Pos += "/incbin/".Length;
                    Expect('(');
                    Skip();
                    var Include = ReadString();
                    Expect(')');
                    Expect(';');
                    var Path = Encoding.UTF8.GetString(Include).TrimEnd('\0');
                    var Full = System.IO.Path.IsPathRooted(Path) ? Path : System.IO.Path.Combine(BaseDir, Path);
                    return File.ReadAllBytes(Full);
                }

                throw new Exception("Unsupported .its property value at " + Pos);
            }

            private byte[] ReadString()
            {
                Expect('"');
                var Bytes = new List<byte>();
                while (true)
                {
                    if (Pos >= Text.Length)
                        throw new Exception("Unterminated string");
                    var C = Text[Pos++];
                    if (C == '"')
                        break;
                    if (C == '\\')
                    {
                        if (Pos >= Text.Length)
                            throw new Exception("Unterminated escape");
                        var E = Text[Pos++];
                        switch (E)
                        {
                            case 'n': Bytes.Add((byte)'\n'); break;
                            case 'r': Bytes.Add((byte)'\r'); break;
                            case 't': Bytes.Add((byte)'\t'); break;
                            case '\\': Bytes.Add((byte)'\\'); break;
                            case '"': Bytes.Add((byte)'"'); break;
                            default: Bytes.Add((byte)E); break;
                        }
                    }
                    else
                        Bytes.Add((byte)C);
                }
                Bytes.Add(0);
                return Bytes.ToArray();
            }

            private long ReadNumber()
            {
                var Word = ReadWord();
                if (Word.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    long Value;
                    if (Int64.TryParse(Word.Substring(2), NumberStyles.HexNumber, null, out Value))
                        return Value;
                }
                else
                {
                    long Value;
                    if (Int64.TryParse(Word, out Value))
                        return Value;
                }

                throw new Exception("Invalid cell value '" + Word + "'");
            }

            private int ReadHexByte()
            {
                var Word = ReadWord();
                int Value;
                if (Int32.TryParse(Word, NumberStyles.HexNumber, null, out Value))
                    return Value;
                throw new Exception("Invalid byte value '" + Word + "'");
            }

            private string Word = "";

            private string ReadWord()
            {
                Skip();
                var Start = Pos;
                while (Pos < Text.Length)
                {
                    var C = Text[Pos];
                    if (Char.IsWhiteSpace(C) || (C == '{') || (C == '}') || (C == ';') || (C == '=') || (C == ',') || (C == '>') || (C == ']'))
                        break;
                    if ((C == '/') && (Pos + 1 < Text.Length) && ((Text[Pos + 1] == '/') || (Text[Pos + 1] == '*')))
                        break;
                    Pos++;
                }
                Word = Text.Substring(Start, Pos - Start);
                return Word;
            }

            private void Skip()
            {
                while (Pos < Text.Length)
                {
                    if (Char.IsWhiteSpace(Text[Pos]))
                    {
                        Pos++;
                        continue;
                    }
                    if ((Text[Pos] == '/') && (Pos + 1 < Text.Length) && (Text[Pos + 1] == '/'))
                    {
                        Pos += 2;
                        while ((Pos < Text.Length) && (Text[Pos] != '\n')) Pos++;
                        continue;
                    }
                    if ((Text[Pos] == '/') && (Pos + 1 < Text.Length) && (Text[Pos + 1] == '*'))
                    {
                        Pos += 2;
                        while ((Pos + 1 < Text.Length) && !((Text[Pos] == '*') && (Text[Pos + 1] == '/'))) Pos++;
                        Pos = Math.Min(Text.Length, Pos + 2);
                        continue;
                    }
                    break;
                }
            }

            private bool Peek(char C) => (Pos < Text.Length) && (Text[Pos] == C);

            private bool StartsWith(string S) => String.CompareOrdinal(Text, Pos, S, 0, S.Length) == 0;

            private void Expect(char C)
            {
                Skip();
                if ((Pos >= Text.Length) || (Text[Pos] != C))
                    throw new Exception("Expected '" + C + "' at " + Pos);
                Pos++;
            }

            private void ReadUntil(char C)
            {
                while ((Pos < Text.Length) && (Text[Pos] != C)) Pos++;
            }
        }
    }
}

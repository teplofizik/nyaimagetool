using NyaFs.Filesystem.Universal.Items;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xunit;
using FileItem = NyaFs.Filesystem.Universal.Items.File;

namespace NyaFsTests
{
    public class NfsProbeTests
    {
        [Fact]
        public void NfsServer_MountAndEdit()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Processor = new ImageProcessor(Parser);
            var Fs = Processor.GetFs(true);

            Fs.Fs.Root.Items.Add(new Dir("etc", 0, 0, 0x1ED));
            var Etc = (Dir)Fs.Fs.Root.Items[0];
            Etc.Items.Add(new FileItem("etc/config", 0, 0, 0x1A4, Encoding.ASCII.GetBytes("hello")));
            Fs.Fs.Root.Items.Add(new FileItem("hello.txt", 0, 0, 0x1A4, Encoding.ASCII.GetBytes("root")));

            var Plugin = new NyaFsNfs.NfsPlugin();
            Plugin.SetOption("port", "22049");
            Plugin.SetOption("mountport", "22048");
            Plugin.SetOption("portmap", "0");
            Plugin.Run(Processor);

            try
            {
                Thread.Sleep(500);

                var Mount = NfsClient.Call(22048, NfsClient.ProgramMount, 3, 1, W =>
                {
                    W.String("/");
                });

                var M = new Reader(Mount);
                Assert.Equal(0u, M.U32());
                var Root = M.Opaque();
                Assert.True(Root.Length > 0);

                var UdpMount = NfsClient.CallUdp(22048, NfsClient.ProgramMount, 3, 1, W => W.String("/"));
                var UM = new Reader(UdpMount);
                Assert.Equal(0u, UM.U32());
                var UdpRoot = UM.Opaque();
                Assert.True(UdpRoot.Length > 0);

                var UdpAttr = NfsClient.CallUdp(22049, NfsClient.ProgramNfs, 3, 1, W => W.Opaque(UdpRoot));
                Assert.Equal(0u, new Reader(UdpAttr).U32());

                var Attr = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 1, W =>
                {
                    W.Opaque(Root);
                });
                var A = new Reader(Attr);
                Assert.Equal(0u, A.U32());
                Assert.Equal(2u, A.U32());

                var Lookup = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 3, W =>
                {
                    W.Opaque(Root);
                    W.String("etc");
                });
                var L = new Reader(Lookup);
                Assert.Equal(0u, L.U32());
                Assert.True(L.Bool());
                var EtcHandle = L.Opaque();

                var ConfigLookup = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 3, W =>
                {
                    W.Opaque(EtcHandle);
                    W.String("config");
                });
                var CL = new Reader(ConfigLookup);
                Assert.Equal(0u, CL.U32());
                Assert.True(CL.Bool());
                var ConfigHandle = CL.Opaque();

                var ReadDir = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 16, W =>
                {
                    W.Opaque(Root);
                    W.U64(0);
                    W.Fixed(new byte[8]);
                    W.U32(8192);
                });
                var D = new Reader(ReadDir);
                Assert.Equal(0u, D.U32());
                Assert.True(D.Bool());
                SkipAttr(D);
                D.Fixed(8);
                var Names = new List<string>();
                while (D.Bool())
                {
                    D.U64();
                    Names.Add(D.String());
                    D.U64();
                }
                D.Bool();

                Assert.Contains(".", Names);
                Assert.Contains("..", Names);
                Assert.Contains("etc", Names);
                Assert.Contains("hello.txt", Names);

                var Read = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 6, W =>
                {
                    W.Opaque(ConfigHandle);
                    W.U64(0);
                    W.U32(1024);
                });
                var R = new Reader(Read);
                Assert.Equal(0u, R.U32());
                R.Bool();
                SkipAttr(R);
                Assert.Equal(5u, R.U32());
                Assert.True(R.Bool());
                Assert.Equal("hello", Encoding.ASCII.GetString(R.Opaque()));

                var Create = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 8, W =>
                {
                    W.Opaque(Root);
                    W.String("new.txt");
                    W.U32(0);
                    W.Bool(true);
                    W.U32(0x1A4);
                    W.Bool(false);
                    W.Bool(false);
                    W.Bool(false);
                    W.U32(0);
                    W.U32(0);
                });
                var C = new Reader(Create);
                Assert.Equal(0u, C.U32());
                Assert.True(C.Bool());
                var NewHandle = C.Opaque();
                C.Bool();
                SkipAttr(C);

                var Write = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 7, W =>
                {
                    W.Opaque(NewHandle);
                    W.U64(0);
                    W.U32(4);
                    W.U32(2);
                    W.Opaque(Encoding.ASCII.GetBytes("data"));
                });
                var Wr = new Reader(Write);
                Assert.Equal(0u, Wr.U32());
                SkipWcc(Wr);
                Assert.Equal(4u, Wr.U32());
                Assert.Equal(2u, Wr.U32());

                var ReadNew = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 6, W =>
                {
                    W.Opaque(NewHandle);
                    W.U64(0);
                    W.U32(1024);
                });
                var RN = new Reader(ReadNew);
                Assert.Equal(0u, RN.U32());
                RN.Bool();
                SkipAttr(RN);
                Assert.Equal(4u, RN.U32());
                RN.Bool();
                Assert.Equal("data", Encoding.ASCII.GetString(RN.Opaque()));

                var Rename = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 14, W =>
                {
                    W.Opaque(Root);
                    W.String("new.txt");
                    W.Opaque(Root);
                    W.String("renamed.txt");
                });
                var Re = new Reader(Rename);
                Assert.Equal(0u, Re.U32());
                SkipWcc(Re);
                SkipWcc(Re);

                var Remove = NfsClient.Call(22049, NfsClient.ProgramNfs, 3, 12, W =>
                {
                    W.Opaque(Root);
                    W.String("renamed.txt");
                });
                var Rm = new Reader(Remove);
                Assert.Equal(0u, Rm.U32());
                SkipWcc(Rm);
            }
            finally
            {
                Plugin.Stop();
            }
        }


        [Fact]
        public void NfsServer_AdditionalOps()
        {
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Processor = new ImageProcessor(Parser);
            var Fs = Processor.GetFs(true);
            Fs.Fs.Root.Items.Add(new FileItem("hello.txt", 0, 0, 0x1A4, Encoding.ASCII.GetBytes("root")));

            var Plugin = new NyaFsNfs.NfsPlugin();
            Plugin.SetOption("port", "22059");
            Plugin.SetOption("mountport", "22058");
            Plugin.SetOption("portmap", "22111");
            Plugin.Run(Processor);

            try
            {
                Thread.Sleep(500);

                var Mount = NfsClient.Call(22058, NfsClient.ProgramMount, 3, 1, W => W.String("/"));
                var M = new Reader(Mount);
                Assert.Equal(0u, M.U32());
                var Root = M.Opaque();

                var MkDir = NfsClient.Call(22059, NfsClient.ProgramNfs, 3, 9, W =>
                {
                    W.Opaque(Root);
                    W.String("dir");
                    W.Bool(true);
                    W.U32(0x1ED);
                    W.Bool(false);
                    W.Bool(false);
                    W.Bool(false);
                    W.U32(0);
                    W.U32(0);
                });
                var MD = new Reader(MkDir);
                Assert.Equal(0u, MD.U32());
                Assert.True(MD.Bool());
                var DirHandle = MD.Opaque();

                var SetAttr = NfsClient.Call(22059, NfsClient.ProgramNfs, 3, 2, W =>
                {
                    W.Opaque(DirHandle);
                    W.Bool(true);
                    W.U32(0x1C0);
                    W.Bool(true);
                    W.U32(1000);
                    W.Bool(true);
                    W.U32(1000);
                    W.Bool(false);
                    W.U32(0);
                    W.U32(0);
                    W.Bool(false);
                });
                var SA = new Reader(SetAttr);
                Assert.Equal(0u, SA.U32());
                SkipWcc(SA);

                var GetAttr = NfsClient.Call(22059, NfsClient.ProgramNfs, 3, 1, W => W.Opaque(DirHandle));
                var GA = new Reader(GetAttr);
                Assert.Equal(0u, GA.U32());
                Assert.Equal(2u, GA.U32());
                Assert.Equal(0x41C0u, GA.U32());
                GA.U32();
                Assert.Equal(1000u, GA.U32());
                Assert.Equal(1000u, GA.U32());

                var ReadDirPlus = NfsClient.Call(22059, NfsClient.ProgramNfs, 3, 17, W =>
                {
                    W.Opaque(Root);
                    W.U64(0);
                    W.Fixed(new byte[8]);
                    W.U32(8192);
                    W.U32(65536);
                });
                var DP = new Reader(ReadDirPlus);
                Assert.Equal(0u, DP.U32());
                Assert.True(DP.Bool());
                SkipAttr(DP);
                DP.Fixed(8);
                var PlusNames = new List<string>();
                while (DP.Bool())
                {
                    DP.U64();
                    PlusNames.Add(DP.String());
                    DP.U64();
                    Assert.True(DP.Bool());
                    SkipAttr(DP);
                    Assert.True(DP.Bool());
                    DP.Opaque();
                }
                DP.Bool();
                Assert.Contains("dir", PlusNames);
                Assert.Contains("hello.txt", PlusNames);

                foreach (var Proc in new uint[] { 18, 19, 20, 21 })
                {
                    var Reply = NfsClient.Call(22059, NfsClient.ProgramNfs, 3, Proc, W =>
                    {
                        W.Opaque(Root);
                        if (Proc == 21)
                        {
                            W.U64(0);
                            W.U32(0);
                        }
                    });
                    Assert.Equal(0u, new Reader(Reply).U32());
                }

                var GetPort = NfsClient.Call(22111, 100000, 2, 3, W =>
                {
                    W.U32(100003);
                    W.U32(3);
                    W.U32(6);
                    W.U32(0);
                });
                Assert.Equal(22059u, new Reader(GetPort).U32());

                var GetPortUdp = NfsClient.CallUdp(22111, 100000, 2, 3, W =>
                {
                    W.U32(100003);
                    W.U32(3);
                    W.U32(17);
                    W.U32(0);
                });
                Assert.Equal(22059u, new Reader(GetPortUdp).U32());

                var RemoveDir = NfsClient.Call(22059, NfsClient.ProgramNfs, 3, 13, W =>
                {
                    W.Opaque(Root);
                    W.String("dir");
                });
                var RD = new Reader(RemoveDir);
                Assert.Equal(0u, RD.U32());
                SkipWcc(RD);
            }
            finally
            {
                Plugin.Stop();
            }
        }
        private static void SkipAttr(Reader R)
        {
            R.U32();
            R.U32();
            R.U32();
            R.U32();
            R.U32();
            R.U64();
            R.U64();
            R.U64();
            R.U64();
            R.U64();
            R.U64();
            R.U64();
            R.U64();
        }

        private static void SkipWcc(Reader R)
        {
            if (R.Bool())
            {
                R.U64();
                R.U64();
                R.U64();
            }

            if (R.Bool())
                SkipAttr(R);
        }

        private static class NfsClient
        {
            public const uint ProgramNfs = 100003;
            public const uint ProgramMount = 100005;

            public static byte[] Call(int Port, uint Program, uint Version, uint Procedure, Action<Writer> Args)
            {
                var W = new Writer();
                W.U32(1);
                W.U32(0);
                W.U32(2);
                W.U32(Program);
                W.U32(Version);
                W.U32(Procedure);
                W.U32(0);
                W.U32(0);
                W.U32(0);
                W.U32(0);
                Args(W);

                var Message = W.ToArray();
                var Header = new byte[4];
                uint Mark = 0x80000000u | (uint)Message.Length;
                Header[0] = (byte)(Mark >> 24);
                Header[1] = (byte)(Mark >> 16);
                Header[2] = (byte)(Mark >> 8);
                Header[3] = (byte)Mark;

                using (var Client = new TcpClient())
                {
                    Client.Connect("127.0.0.1", Port);
                    var Stream = Client.GetStream();
                    Stream.Write(Header, 0, 4);
                    Stream.Write(Message, 0, Message.Length);
                    Stream.Flush();

                    var ReplyHeader = ReadExactly(Stream, 4);
                    uint ReplyMark = ((uint)ReplyHeader[0] << 24) | ((uint)ReplyHeader[1] << 16) |
                                     ((uint)ReplyHeader[2] << 8) | ReplyHeader[3];
                    var Reply = ReadExactly(Stream, (int)(ReplyMark & 0x7FFFFFFF));

                    var R = new Reader(Reply);
                    Assert.Equal(1u, R.U32());
                    Assert.Equal(1u, R.U32());
                    Assert.Equal(0u, R.U32());
                    Assert.Equal(0u, R.U32());
                    Assert.Equal(0u, R.U32());
                    Assert.Equal(0u, R.U32());
                    return R.Skip(R.Position);
                }
            }

            public static byte[] CallUdp(int Port, uint Program, uint Version, uint Procedure, Action<Writer> Args)
            {
                var W = new Writer();
                W.U32(1);
                W.U32(0);
                W.U32(2);
                W.U32(Program);
                W.U32(Version);
                W.U32(Procedure);
                W.U32(0);
                W.U32(0);
                W.U32(0);
                W.U32(0);
                Args(W);

                using (var Client = new System.Net.Sockets.UdpClient())
                {
                    var Message = W.ToArray();
                    Client.Send(Message, Message.Length, "127.0.0.1", Port);
                    var Remote = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                    var Reply = Client.Receive(ref Remote);
                    var R = new Reader(Reply);
                    Assert.Equal(1u, R.U32());
                    Assert.Equal(1u, R.U32());
                    Assert.Equal(0u, R.U32());
                    Assert.Equal(0u, R.U32());
                    Assert.Equal(0u, R.U32());
                    Assert.Equal(0u, R.U32());
                    return R.Skip(R.Position);
                }
            }

            private static byte[] ReadExactly(Stream Stream, int Length)
            {
                var Result = new byte[Length];
                int Position = 0;
                while (Position < Length)
                {
                    int Read = Stream.Read(Result, Position, Length - Position);
                    if (Read <= 0)
                        throw new EndOfStreamException();
                    Position += Read;
                }
                return Result;
            }
        }

        private sealed class Writer
        {
            private readonly List<byte> Data = new List<byte>();

            public void U32(uint Value) => Data.AddRange(new[]
            {
                (byte)(Value >> 24), (byte)(Value >> 16), (byte)(Value >> 8), (byte)Value
            });

            public void U64(ulong Value)
            {
                U32((uint)(Value >> 32));
                U32((uint)Value);
            }

            public void Bool(bool Value) => U32(Value ? 1u : 0u);

            public void String(string Value) => Opaque(Encoding.UTF8.GetBytes(Value));

            public void Opaque(byte[] Value)
            {
                U32((uint)Value.Length);
                Data.AddRange(Value);
                int Pad = (4 - (Value.Length & 3)) & 3;
                for (int i = 0; i < Pad; i++)
                    Data.Add(0);
            }

            public void Fixed(byte[] Value)
            {
                Data.AddRange(Value);
                int Pad = (4 - (Value.Length & 3)) & 3;
                for (int i = 0; i < Pad; i++)
                    Data.Add(0);
            }

            public byte[] ToArray() => Data.ToArray();
        }

        private sealed class Reader
        {
            private readonly byte[] Data;
            public int Position { get; private set; }

            public Reader(byte[] Data, int Position = 0)
            {
                this.Data = Data;
                this.Position = Position;
            }

            public uint U32()
            {
                uint Value = ((uint)Data[Position] << 24) | ((uint)Data[Position + 1] << 16) |
                             ((uint)Data[Position + 2] << 8) | Data[Position + 3];
                Position += 4;
                return Value;
            }

            public ulong U64() => ((ulong)U32() << 32) | U32();

            public bool Bool() => U32() != 0;

            public byte[] Opaque() => Fixed((int)U32());

            public string String() => Encoding.UTF8.GetString(Opaque());

            public byte[] Fixed(int Length)
            {
                var Result = new byte[Length];
                Array.Copy(Data, Position, Result, 0, Length);
                Position += Length + ((4 - (Length & 3)) & 3);
                return Result;
            }

            public byte[] Skip(int Offset)
            {
                var Result = new byte[Data.Length - Offset];
                Array.Copy(Data, Offset, Result, 0, Result.Length);
                return Result;
            }
        }
    }
}

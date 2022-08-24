using CpioLib.IO.Script;
using CpioLib.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CpioLib.IO
{
    public static class CpioExtractor
    {
        public static void GenerateScript(CpioArchive Archive, string Dir, string Commands)
        {
            var Steps = new List<ScriptStep>();

            if (Directory.Exists(Dir))
            {
                foreach (var F in Archive.Files)
                {
                    switch (F.FileType)
                    {
                        case CpioModeFileType.C_ISDIR: // Dir
                            Steps.Add(new ScriptStepDir(F.Path, F.StrMode, F.UserId, F.GroupId));
                            break;
                        case CpioModeFileType.C_ISREG: // File
                            var FN = Path.Combine(Dir, F.Path);
                            Steps.Add(new ScriptStepFile(F.Path, FN, F.StrMode, F.UserId, F.GroupId));
                            break;
                        case CpioModeFileType.C_ISLNK: // Link
                            var To = UTF8Encoding.UTF8.GetString(F.Content);
                            Steps.Add(new ScriptStepSLink(F.Path, To, F.StrMode, F.UserId, F.GroupId));
                            break;
                        case CpioModeFileType.C_ISCHR: // Node [c]
                            Steps.Add(new ScriptStepNod(F.Path, F.StrMode, F.UserId, F.GroupId, "c", F.RMajor, F.RMinor));
                            break;
                        default:
                            Console.WriteLine($"{F.Path}: {GetFileType(F.FileType)}");
                            break;
                    }
                }
            }

            var Lines = Array.ConvertAll(Steps.ToArray(), S => S.CommandLine);
            File.WriteAllLines(Commands, Lines);
            Console.WriteLine($"Script is writed to {Commands}");
        }

        public static void Extract(CpioArchive Archive, string Dir)
        {
            if(!Directory.Exists(Dir))
                Directory.CreateDirectory(Dir);
            
            foreach(var F in Archive.Files)
            {
                switch(F.FileType)
                {
                    case CpioModeFileType.C_ISDIR: // Dir
                        var D = Path.Combine(Dir, F.Path);
                        if (!Directory.Exists(D))
                            Directory.CreateDirectory(D);

                        Console.WriteLine($"D {F.Path}");
                        break;
                    case CpioModeFileType.C_ISREG: // File
                        var FN = Path.Combine(Dir, F.Path);
                        var Data = F.Content;

                        Console.WriteLine($"F {F.Path}");
                        File.WriteAllBytes(FN, Data);
                        break;
                    default:
                        // Console.WriteLine($"{F.Path}: {GetFileType(F.FileType)}");
                        break;
                }
            }
        }

        private static string GetFileType(CpioModeFileType Type)
        {
            switch(Type)
            {
                case CpioModeFileType.C_ISBLK: return "BLK";
                case CpioModeFileType.C_ISCHR: return "CHR";
                case CpioModeFileType.C_ISCTG: return "CTG";
                case CpioModeFileType.C_ISDIR: return "DIR";
                case CpioModeFileType.C_ISFIFO: return "FIFO";
                case CpioModeFileType.C_ISLNK: return "LNK";
                case CpioModeFileType.C_ISREG: return "REG";
                case CpioModeFileType.C_ISSOCK: return "SOCK";
                default: return $"{Type}";
            }
        }
    }
}

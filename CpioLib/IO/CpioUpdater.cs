using CpioLib.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CpioLib.IO
{
    public static class CpioUpdater
    {
        public static void Info(ref CpioArchive Archive)
        {
            foreach (var F in Archive.Files)
            {
                Console.WriteLine($"{F.Path}: {F.StrMode} m:{F.Mode:x02} in:{F.INode} links:{F.NumLink} maj:{F.Major} min:{F.Minor} rmaj:{F.RMajor} rmin:{F.RMinor}");
            }
        }

        public static void UpdateArchive(ref CpioArchive Archive, string RootDir, string CommandsFile)
        {
            if (RootDir != null)
            {
                Console.WriteLine($"Root dir update {RootDir}");
                var Processed = new List<string>();
                var Filenames = Array.ConvertAll(Archive.Files.ToArray(), F => F.Path);
                foreach (var F in Filenames)
                {
                    var LocalPath = Path.Combine(RootDir, F);

                    if (File.Exists(LocalPath))
                    {
                        Console.WriteLine($"Update  {F}");

                        Archive.UpdateFile(F, LocalPath);
                        Processed.Add(F);
                    }
                }

                var UpdateDirs = Directory.GetDirectories(RootDir, "*", SearchOption.AllDirectories);
                foreach (var F in UpdateDirs)
                {
                    // ./root/etc\init.d\pgnand.sh
                    var ConvertedF = F.Substring(RootDir.Length).Replace('\\', '/');

                    if (!Archive.Exists(ConvertedF))
                    {
                        Archive.AddDir(ConvertedF, F);
                        Console.WriteLine($"Add dir {ConvertedF}");

                        Processed.Add(ConvertedF);
                    }
                }

                var UpdateFiles = Directory.GetFiles(RootDir, "*", SearchOption.AllDirectories);
                foreach (var F in UpdateFiles)
                {
                    // ./root/etc\init.d\pgnand.sh
                    var ConvertedF = F.Substring(RootDir.Length).Replace('\\', '/');

                    if (!Processed.Contains(ConvertedF))
                    {
                        var Content = File.ReadAllBytes(F);

                        if (!Archive.Exists(ConvertedF))
                        {
                            Archive.AddFile(ConvertedF, F);
                            Console.WriteLine($"Add     {ConvertedF}");
                            Processed.Add(ConvertedF);
                        }
                    }
                }
            }

            ProcessCommands(ref Archive, CommandsFile);
        }

        private static string FilterPath(string Filename)
        {
            if (Filename.IndexOf('/') == 0)
                Filename = Filename.Substring(1);
            return Filename;
        }

        private static UInt32 ConvertMode(string Mode)
        {
            UInt32 ModeX = 0;
            for(int i = 0; i < 3; i++)
            {
                int Offset = i * 3;

                for(int c = 0; c < 3; c++)
                {
                    var C = Mode[Offset + c];

                    if (C == 'r') ModeX |= 4U << ((2 - i) * 4);
                    if (C == 'w') ModeX |= 2U << ((2 - i) * 4);
                    if (C == 'x') ModeX |= 1U << ((2 - i) * 4);
                }
            }
            return ModeX;
        }

        private static string ConvertModeToString(UInt32 Mode)
        {
            var Res = "";
            for(int i = 0; i < 3; i++)
            {
                UInt32 Part = (Mode >> (2 - i) * 4) & 0x7;

                Res += ((Part & 0x04) != 0) ? "r" : "-";
                Res += ((Part & 0x02) != 0) ? "w" : "-";
                Res += ((Part & 0x01) != 0) ? "x" : "-";
            }
            return Res;
        }

        private static bool ProcessCommands(ref CpioArchive Archive, string CommandsFile)
        {
            if ((CommandsFile != null) && File.Exists(CommandsFile))
            {
                Console.WriteLine($"Commands {CommandsFile}");
                var CommandsList = File.ReadAllLines(CommandsFile);
                foreach (var Cmd in CommandsList)
                {
                    var TCmd = Cmd.Trim();
                    if (TCmd.StartsWith('#')) continue;

                    var Parts = TCmd.Split(new char[] { ' ' });
                    ProcessCommand(CommandsFile, Parts, Archive);
                }

                return true;
            }
            return false;
        }

        private static void LogOk(string Text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void LogError(string Text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void LogWarning(string Text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Text);
            Console.ForegroundColor = ConsoleColor.White;
        }


        private static void ProcessCommand(string CmdPath, string[] Command, CpioArchive Archive)
        {
            var Sh = Archive.GetFile("usr/bin/passwd");
            if (Command.Length >= 2)
            {
                var Cmd = Command[0];
                var Path = FilterPath(Command[1]);
                switch (Cmd)
                {
                    case "rm":
                        {
                            if (Path.Length > 0)
                            {
                                var ExFile = Archive.GetFile(Path);
                                if (ExFile != null)
                                {
                                    Archive.Delete(Path);
                                    LogOk($"Delete  {Path}");
                                }
                                else
                                {
                                    if(Path.Last() == '/')
                                    {
                                        var Files = Archive.DeleteFolder(Path);
                                        if(Files.Length > 0)
                                        {
                                            foreach (var F in Files)
                                                LogOk($"Delete  {F}");
                                        }
                                        else
                                            LogError($"Delete  {Path}: file not found");
                                    }
                                }
                            }
                            else
                            {
                                LogError($"Delete: path is empty");
                            }
                        }
                        break;
                    case "chmod":
                        if (Command.Length == 3)
                        {
                            var Mode = (Command[2].Length == 9) ? ConvertMode(Command[2]) : Convert.ToUInt32(Command[2], 16) & 0xFFF;
                            var FileList = Archive.ChMod(Path, Mode);
                            if (FileList.Length == 0)
                                LogError($"ChMod  {Path}: item not found");
                            else
                                Array.ForEach(FileList, F => LogOk($"ChMod    {F}: {ConvertModeToString(Mode)}"));
                        }
                        else
                        {
                            Console.WriteLine($"ChOwn    {Path}: need 2 arguments, {Command.Length - 1} given");
                        }
                        break;
                    case "chown":
                        if (Command.Length == 3)
                        {
                            var OwnerS = Command[2];

                            if (OwnerS.IndexOf(':') > 0)
                            {
                                var Parts = OwnerS.Split(new char[] { ':' });
                                if (Parts.Length == 2)
                                {
                                    var Owner = Convert.ToUInt32(Parts[0]);
                                    var Group = Convert.ToUInt32(Parts[1]);
                                    Archive.ChOwn(Path, Owner);
                                    var FileList = Archive.ChGroup(Path, Group);
                                    if (FileList.Length == 0)
                                        LogError($"ChOwn  {Path}: item not found");
                                    else
                                        Array.ForEach(FileList, F => LogOk($"ChOwn    {F}: {Owner}:{Group}"));
                                }
                            }
                            else
                            {
                                var Owner = Convert.ToUInt32(Command[2]);
                                var FileList = Archive.ChOwn(Path, Owner);
                                if (FileList.Length == 0)
                                    LogError($"ChOwn  {Path}: item not found");
                                else
                                    Array.ForEach(FileList, F => LogOk($"ChOwn    {F}: {Owner}"));
                            }
                        }
                        else
                        {
                            LogError($"ChOwn    {Path}: need 2 arguments, {Command.Length - 1} given");
                        }
                        break;
                    case "group":
                        if (Command.Length == 3)
                        {
                            var Group = Convert.ToUInt32(Command[2]);
                            var FileList = Archive.ChGroup(Path, Group);
                            if (FileList.Length == 0)
                                LogError($"Group  {Path}: item not found");
                            else
                                Array.ForEach(FileList, F => LogOk($"Group    {F}: {Group}"));
                        }
                        else
                        {
                            LogError($"Group    {Path}: need 2 arguments, {Command.Length - 1} given");
                        }
                        break;
                    case "dir":
                        // dir /mnt 755 0 0
                        // file [path] [mode] [uid] [gid]
                        if (Command.Length == 5)
                        {
                            var Mode = (Command[2].Length == 9) ? ConvertMode(Command[2]) : Convert.ToUInt32(Command[2], 16) & 0xFFF;
                            var Owner = Convert.ToUInt32(Command[3]);
                            var Group = Convert.ToUInt32(Command[4]);

                            var ExDir = Archive.GetFile(Path);
                            if (ExDir != null)
                            {
                                if (ExDir.FileType == CpioModeFileType.C_ISDIR)
                                {
                                    LogWarning($"Dir     {Path} already exists");
                                }
                                else
                                {
                                    LogError($"Dir    {Path}: node is not directory");
                                }
                            }
                            else
                            {
                                Archive.AddDir(Path, null);
                                Archive.ChMod(Path, Mode);
                                Archive.ChOwn(Path, Owner);
                                Archive.ChGroup(Path, Group);
                                LogOk($"Dir     {Path}: m:{ ConvertModeToString(Mode) } u:{Owner} g:{Group}");
                            }
                        }
                        else
                        {
                            LogError($"Dir     {Path}: need 4 arguments, {Command.Length - 1} given");
                        }
                        break;
                    case "nod":
                        // nod dev/console rw--w--w- 0 0 c 5 1
                        // nod [path] [mode] [uid] [gid] [type] [maj] [min]
                        if (Command.Length == 8)
                        {
                            var Mode = (Command[2].Length == 9) ? ConvertMode(Command[2]) : Convert.ToUInt32(Command[2], 16) & 0xFFF;
                            var Owner = Convert.ToUInt32(Command[3]);
                            var Group = Convert.ToUInt32(Command[4]);

                            var Type = Command[5];
                            var Maj = Convert.ToUInt32(Command[6]);
                            var Min = Convert.ToUInt32(Command[7]);
                            if (!String.Equals(Type, "c"))
                            {
                                LogError($"Nod     {Path}: unsupported node type {Type}");
                            }
                            else
                            {
                                var ExFile = Archive.GetFile(Path);
                                if (ExFile != null)
                                {
                                    LogError($"Nod    {Path}: node is already exists");
                                }
                                else
                                {
                                    Archive.AddNod(Path, Maj, Min);
                                    Archive.ChMod(Path, Mode);
                                    Archive.ChOwn(Path, Owner);
                                    Archive.ChGroup(Path, Group);

                                    LogOk($"Nod    {Path}: m:{ ConvertModeToString(Mode) } u:{Owner} g:{Group} t:{Type} mj:{Maj} mn:{Min}");
                                }
                            }
                        }
                        else
                        {
                            LogError($"Nod     {Path}: need 7 arguments, {Command.Length - 1} given");
                        }
                        break;
                    case "file":
                        // file /init ./content/init 755 0 0
                        // file [path] [filepath]
                        // file [path] [filepath] [mode] [uid] [gid]
                        if (Command.Length == 6)
                        {
                            var LocalPath = Command[2];
                            var Mode = (Command[3].Length == 9) ? ConvertMode(Command[3]) : Convert.ToUInt32(Command[3], 16) & 0xFFF;
                            var Owner = Convert.ToUInt32(Command[4]);
                            var Group = Convert.ToUInt32(Command[5]);
                            if (File.Exists(LocalPath))
                            {
                                var ExFile = Archive.GetFile(Path);
                                if (ExFile != null)
                                {
                                    if (ExFile.FileType == CpioModeFileType.C_ISREG)
                                    {
                                        Archive.UpdateFile(Path, LocalPath);
                                        Archive.ChMod(Path, Mode);
                                        Archive.ChOwn(Path, Owner);
                                        Archive.ChGroup(Path, Group);

                                        LogOk($"File    {Path}: updated by {LocalPath} m:{ ConvertModeToString(Mode) } u:{Owner} g:{Group}");
                                    }
                                    else
                                        LogError($"File    {Path}: file is not regular file");
                                }
                                else
                                {
                                    Archive.AddFile(Path, LocalPath);
                                    Archive.ChMod(Path, Mode);
                                    Archive.ChOwn(Path, Owner);
                                    Archive.ChGroup(Path, Group);

                                    LogOk($"File    {Path}: {LocalPath} m:{ ConvertModeToString(Mode) } u:{Owner} g:{Group}");
                                }
                            }
                            else
                            {
                                LogError($"File    {Path}: file {LocalPath} not found");
                            }
                        }
                        else if (Command.Length == 3)
                        {
                            var LocalPath = Command[2];
                            if (File.Exists(LocalPath))
                            {
                                var ExFile = Archive.GetFile(Path);
                                if (ExFile != null)
                                {
                                    if (ExFile.FileType == CpioModeFileType.C_ISREG)
                                    {
                                        Archive.UpdateFile(Path, LocalPath);

                                        LogOk($"File    {Path}: updated by {LocalPath}");
                                    }
                                    else
                                        LogError($"File    {Path}: file is not regular file");
                                }
                                else
                                {
                                    LogError($"File    {Path}: file not found, unable to modify");
                                }
                            }
                            else
                            {
                                LogError($"File    {Path}: file {LocalPath} not found");
                            }
                        }
                        else
                        {
                            LogError($"File    {Path}: need 5 or 2 arguments, {Command.Length - 1} given");
                        }
                        break;
                    case "slink":
                        // slink /usr/bin/scp /usr/sbin/dropbearmulti 777 0 0
                        if (Command.Length == 6)
                        {
                            var ToPath = Command[2];
                            var Mode = (Command[3].Length == 9) ? ConvertMode(Command[3]) : Convert.ToUInt32(Command[3], 16) & 0xFFF;
                            var Owner = Convert.ToUInt32(Command[4]);
                            var Group = Convert.ToUInt32(Command[5]);

                            var ExLink = Archive.GetFile(Path);
                            if (ExLink != null)
                            {
                                if (ExLink.FileType == CpioModeFileType.C_ISLNK)
                                {
                                    Archive.UpdateSLink(Path, ToPath);
                                    LogOk($"SLink   {Path}: {ToPath} updated");
                                }
                                else
                                    LogError($"File    {Path}: file is not symlink");
                            }
                            else
                            {
                                Archive.AddSLink(Path, ToPath);
                                Archive.ChMod(Path, Mode);
                                Archive.ChOwn(Path, Owner);
                                Archive.ChGroup(Path, Group);

                                LogOk($"SLink   {Path}: {ToPath} m:{ ConvertModeToString(Mode) } u:{Owner} g:{Group}");
                            }
                        }
                        else if (Command.Length == 3)
                        {
                            var ToPath = Command[2];

                            var ExLink = Archive.GetFile(Path);
                            if (ExLink != null)
                            {
                                if (ExLink.FileType == CpioModeFileType.C_ISLNK)
                                {
                                    Archive.UpdateSLink(Path, ToPath);
                                    LogOk($"SLink   {Path}: {ToPath} updated");
                                }
                                else
                                    LogError($"File    {Path}: file is not symlink");
                            }
                            else
                            {
                                LogError($"File    {Path}: file not found");
                            }
                        }
                        else
                        {
                            LogError($"SLink   {Path}: need 5 or 2 arguments, {Command.Length - 1} given");
                        }
                        break;
                    case "include":
                        if (!ProcessCommands(ref Archive, Path))
                        {
                            var RelPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(CmdPath), Path);
                            if (!ProcessCommands(ref Archive, RelPath))
                            {
                                LogError($"Include {Path}: not found");
                            }
                        }
                        break;
                    case "echo":
                        var Text = String.Join(' ', Command.Skip(1).ToArray());
                        Console.WriteLine(Text);
                        break;
                    default:
                        LogError($"Unrecognized command: {String.Join(" ", Command)}");
                        break;

                }
            }
            else if (Command.Length == 1)
            {
                var Cmd = Command[0];

                switch (Cmd)
                {
                    case "clear":
                        Archive.Clear();
                        LogOk($"Removed all files...");
                        break;
                    case "":
                        break;
                    default:
                        LogError($"Unrecognized command: {String.Join(" ", Command)}");
                        break;
                }
            }
        }
    }
}

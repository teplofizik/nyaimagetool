using NyaFs.Filesystem.Universal.Identity;
using NyaFs.ImageFormat.Elements.Fs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NyaFsLinux.Linux.Users
{
    internal static class UserLookup
    {
        public const string ShadowPath = "/etc/shadow";

        public static User[] Load(LinuxFilesystem Fs)
        {
            var Db = Fs.Identities;
            Db.Load();

            var Shadow = ReadLines(Fs, ShadowPath);
            return Db.Users.Select(U => new User(U, FindShadow(U.Name, Shadow))).ToArray();
        }

        public static string[] ReadLines(LinuxFilesystem Fs, string Path)
        {
            try
            {
                var File = Fs.GetElement(Path) as NyaFs.Filesystem.Universal.Items.File;
                if (File == null)
                    return null;

                return Encoding.UTF8.GetString(File.Content ?? new byte[0])
                    .Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static void WriteLines(LinuxFilesystem Fs, string Path, string[] Lines)
        {
            var File = Fs.GetElement(Path) as NyaFs.Filesystem.Universal.Items.File;
            if (File == null)
                return;

            File.Content = Encoding.UTF8.GetBytes(String.Join("\n", Lines));
            File.Modified = DateTime.Now;
        }

        public static void AddShadowEntry(LinuxFilesystem Fs, string Name, string Hash)
        {
            var Lines = ReadLines(Fs, ShadowPath);
            if (Lines == null)
                return;

            var Entry = Name + ":" + Hash + ":::::::";
            var List = Lines.ToList();
            var Index = List.FindIndex(L => L.StartsWith(Name + ":"));
            if (Index >= 0)
                List[Index] = Entry;
            else if ((List.Count > 0) && (List[List.Count - 1].Length == 0))
                List.Insert(List.Count - 1, Entry);
            else
                List.Add(Entry);

            WriteLines(Fs, ShadowPath, List.ToArray());
        }

        public static bool SetLock(LinuxFilesystem Fs, string Name, bool Lock, out string Error)
        {
            Error = null;
            var Lines = ReadLines(Fs, ShadowPath);
            if (Lines == null)
            {
                Error = "Filesystem does not contain /etc/shadow";
                return false;
            }

            for (var I = 0; I < Lines.Length; I++)
            {
                if (!Lines[I].StartsWith(Name + ":"))
                    continue;

                var Parts = Lines[I].Split(new[] { ':' }, StringSplitOptions.None);
                if (Parts.Length < 2)
                    break;

                if (Lock)
                {
                    if (!Parts[1].StartsWith("!"))
                        Parts[1] = "!" + Parts[1];
                }
                else if (Parts[1].StartsWith("!"))
                    Parts[1] = Parts[1].Substring(1);

                Lines[I] = String.Join(":", Parts);
                WriteLines(Fs, ShadowPath, Lines);
                return true;
            }

            Error = "User '" + Name + "' is not found in /etc/shadow";
            return false;
        }
        public static void RemoveShadowEntry(LinuxFilesystem Fs, string Name)
        {
            var Lines = ReadLines(Fs, ShadowPath);
            if (Lines == null)
                return;

            WriteLines(Fs, ShadowPath, Lines.Where(L => !L.StartsWith(Name + ":")).ToArray());
        }

        public static string FindShadow(string Name, string[] Shadow)
        {
            if (Shadow == null)
                return null;

            return Shadow.FirstOrDefault(L => L.StartsWith(Name + ":"));
        }
    }
}

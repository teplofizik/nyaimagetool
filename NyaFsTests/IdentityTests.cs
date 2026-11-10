using System;
using NyaFs.Filesystem.Universal;
using NyaFs.Filesystem.Universal.Identity;
using Dir = NyaFs.Filesystem.Universal.Items.Dir;
using File = NyaFs.Filesystem.Universal.Items.File;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Commands;
using NyaFs.Processor.Scripting.Commands.Fs;
using System.IO;
using System.Text;
using Xunit;

namespace NyaFsTests
{
    public class IdentityTests
    {
        private static LinuxFilesystem FsWithIdentity()
        {
            var Fs = new LinuxFilesystem();
            var Etc = new Dir("etc", 0, 0, 493);
            Fs.Fs.Root.Items.Add(Etc);

            Etc.Items.Add(new File("etc/passwd", 0, 0, 420,
                Encoding.ASCII.GetBytes("root:x:0:0:root:/root:/bin/sh\nuser:x:1000:1000:User:/home/user:/bin/sh\n")));
            Etc.Items.Add(new File("etc/group", 0, 0, 420,
                Encoding.ASCII.GetBytes("root:x:0:\nusers:x:1000:user\n")));
            Etc.Items.Add(new File("etc/hostname", 0, 0, 420, Encoding.ASCII.GetBytes("nyafs")));

            return Fs;
        }

        [Fact]
        public void IdentityDatabase_Loads_And_Resolves_Names()
        {
            var Fs = FsWithIdentity();
            var Db = Fs.Identities;

            Assert.True(Db.Load());
            Assert.Equal(2, Db.Users.Count);
            Assert.Equal("root", Db.GetUserName(0));
            Assert.Equal("users", Db.GetGroupName(1000));

            uint Id;
            Assert.True(Db.TryGetUserId("user", out Id));
            Assert.Equal(1000u, Id);
            Assert.True(Db.TryGetUserId("0", out Id));
            Assert.Equal(0u, Id);
            Assert.False(Db.TryGetUserId("missing", out Id));
        }

        [Fact]
        public void Chown_Accepts_Name_And_NameGroup()
        {
            var Fs = FsWithIdentity();
            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.SetFs(Fs);

            var Res = new Chown().Get(new ScriptArgs(0, new[] { "etc", "user:users" })).Exec(Proc);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);
            Assert.Equal(1000u, Fs.GetElement("etc").User);
            Assert.Equal(1000u, Fs.GetElement("etc").Group);
        }

        [Fact]
        public void IdentityDatabase_Adds_Users_And_Groups_And_Persists()
        {
            var Fs = FsWithIdentity();
            var Db = Fs.Identities;
            string Error;

            Assert.True(Db.AddUser("app", 2000, 2000, "", "/home/app", "/bin/sh", out Error), Error);
            Assert.True(Db.AddGroup("app", 2000, out Error), Error);
            Assert.True(Db.AddUserToGroup("app", "app", out Error), Error);
            Assert.True(Db.Save(out Error), Error);

            var Check = new FsIdentityDatabase(Fs.Fs);
            Assert.True(Check.Load());
            Assert.Equal("app", Check.GetUserName(2000));
            Assert.Equal("app", Check.GetGroupName(2000));
            Assert.Contains("app", Check.FindGroup("app").Members);

            var Passwd = Encoding.UTF8.GetString(((File)Fs.GetElement("etc/passwd")).Content);
            Assert.Contains("app:x:2000:2000::/home/app:/bin/sh", Passwd);
        }

        [Fact]
        public void Identity_User_Survives_Cpio_Store()
        {
            var Fs = FsWithIdentity();
            var Db = Fs.Identities;
            string Error;
            Assert.True(Db.AddUser("persist", 3000, 3000, "", "/home/persist", "/bin/sh", out Error), Error);
            Assert.True(Db.Save(out Error), Error);

            var Parser = new ScriptParser(new ScriptBaseAll());
            var Proc = new ImageProcessor(Parser, new string[0]);
            Proc.SetFs(Fs);

            var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyafs-identity-" + Guid.NewGuid().ToString("N") + ".cpio");
            var Res = new Store().Get(new ScriptArgs(1, new[] { Path, "ramfs", "cpio" })).Exec(Proc);
            Assert.Equal(ScriptStepStatus.Ok, Res.Status);

            var ReadParser = new ScriptParser(new ScriptBaseAll());
            var ReadProc = new ImageProcessor(ReadParser, new string[0]);
            ReadProc.Process(ReadParser.ParseScript("t", new[] { "load \"" + Path + "\"" }));

            var ReadDb = ReadProc.GetFs().Identities;
            Assert.True(ReadDb.Load());
            Assert.Equal("persist", ReadDb.GetUserName(3000));
        }

        [Fact]
        public void Removing_User_Removes_Group_Membership()
        {
            var Fs = FsWithIdentity();
            var Db = Fs.Identities;
            string Error;

            Assert.True(Db.RemoveUser("user", out Error), Error);
            Assert.True(Db.Save(out Error), Error);

            var Check = new FsIdentityDatabase(Fs.Fs);
            Check.Load();
            Assert.Null(Check.FindUser("user"));
            Assert.DoesNotContain("user", Check.FindGroup("users").Members);
        }
    }
}

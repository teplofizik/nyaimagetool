using NyaExtensions.Array;
using FreeSFtpSharp;
using FxSsh;
using FxSsh.Services;
using NyaFs.Filesystem.Universal.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NyaFsSftp.Ssh
{
    class NyaSshService
    {
        SshServer Server;
        NyaFs.Processor.ImageProcessor Processor;

        public NyaSshService(int Port, NyaFs.Processor.ImageProcessor Processor)
        {
            this.Processor = Processor;

            Server = new SshServer(new SshServerSettings(System.Net.IPAddress.Any, Port, "NyaImageTool"));
            // Test keys
            Server.AddHostKey("ssh-rsa", "BwIAAACkAABSU0EyAAQAAAEAAQADKjiW5UyIad8ITutLjcdtejF4wPA1dk1JFHesDMEhU9pGUUs+HPTmSn67ar3UvVj/1t/+YK01FzMtgq4GHKzQHHl2+N+onWK4qbIAMgC6vIcs8u3d38f3NFUfX+lMnngeyxzbYITtDeVVXcLnFd7NgaOcouQyGzYrHBPbyEivswsnqcnF4JpUTln29E1mqt0a49GL8kZtDfNrdRSt/opeexhCuzSjLPuwzTPc6fKgMc6q4MBDBk53vrFY2LtGALrpg3tuydh3RbMLcrVyTNT+7st37goubQ2xWGgkLvo+TZqu3yutxr1oLSaPMSmf9bTACMi5QDicB3CaWNe9eU73MzhXaFLpNpBpLfIuhUaZ3COlMazs7H9LCJMXEL95V6ydnATf7tyO0O+jQp7hgYJdRLR3kNAKT0HU8enE9ZbQEXG88hSCbpf1PvFUytb1QBcotDy6bQ6vTtEAZV+XwnUGwFRexERWuu9XD6eVkYjA4Y3PGtSXbsvhwgH0mTlBOuH4soy8MV4dxGkxM8fIMM0NISTYrPvCeyozSq+NDkekXztFau7zdVEYmhCqIjeMNmRGuiEo8ppJYj4CvR1hc8xScUIw7N4OnLISeAdptm97ADxZqWWFZHno7j7rbNsq5ysdx08OtplghFPx4vNHlS09LwdStumtUel5oIEVMYv+yWBYSPPZBcVY5YFyZFJzd0AOkVtUbEbLuzRs5AtKZG01Ip/8+pZQvJvdbBMLT1BUvHTrccuRbY03SHIaUM3cTUc=");
            Server.AddHostKey("ssh-dss", "BwIAAAAiAABEU1MyAAQAAG+6KQWB+crih2Ivb6CZsMe/7NHLimiTl0ap97KyBoBOs1amqXB8IRwI2h9A10R/v0BHmdyjwe0c0lPsegqDuBUfD2VmsDgrZ/i78t7EJ6Sb6m2lVQfTT0w7FYgVk3J1Deygh7UcbIbDoQ+refeRNM7CjSKtdR+/zIwO3Qub2qH+p6iol2iAlh0LP+cw+XlH0LW5YKPqOXOLgMIiO+48HZjvV67pn5LDubxru3ZQLvjOcDY0pqi5g7AJ3wkLq5dezzDOOun72E42uUHTXOzo+Ct6OZXFP53ZzOfjNw0SiL66353c9igBiRMTGn2gZ+au0jMeIaSsQNjQmWD+Lnri39n0gSCXurDaPkec+uaufGSG9tWgGnBdJhUDqwab8P/Ipvo5lS5p6PlzAQAAACqx1Nid0Ea0YAuYPhg+YolsJ/ce");

            Server.ConnectionAccepted += Server_ConnectionAccepted;
            Server.ExceptionRaised += Server_ExceptionRaised;

        }

        private void Server_ExceptionRaised(object sender, Exception e)
        {
            Console.WriteLine($"Server_ExceptionRaised");
            while(e != null)
            {
                Console.WriteLine("================");
                Console.WriteLine(e.Source);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                e = e.InnerException;
            }
        }

        public void Start()
        {
            Server.Start();
        }

        public void Stop()
        {
            Server.Stop();
        }

        void Server_ConnectionAccepted(object sender, Session e)
        {
            e.ServiceRegistered += e_ServiceRegistered;
        }

        void e_ServiceRegistered(object sender, SshService e)
        {
            var session = (Session)sender;

            if (e is UserAuthService)
            {
                var service = (UserAuthService)e;
                service.UserAuth += service_Userauth;
            }
            else if (e is ConnectionService)
            {
                var service = (ConnectionService)e;
                service.SessionRequest += Service_SessionRequest;

                service.CommandOpened += service_CommandOpened;
                service.EnvReceived += service_EnvReceived;
                service.PtyReceived += service_PtyReceived;
            }
        }

        private void Service_SessionRequest(object sender, SessionRequestedArgs e)
        {
            if (e.SubSystemName == "sftp")
            {
                InitializeSftp(e.Channel, e.AttachedUserAuthArgs.Username);
            }
            else
            {
                e.Channel.SendData(Encoding.UTF8.GetBytes($"You ran {e.CommandText}\n"));
                e.Channel.SendClose();
            }
        }

        void service_PtyReceived(object sender, PtyArgs e)
        {

        }

        void service_EnvReceived(object sender, EnvironmentArgs e)
        {

        }

        void service_Userauth(object sender, UserAuthArgs e)
        {
            e.Result = true;
        }

        void service_CommandOpened(object sender, CommandRequestedArgs e)
        {
            if (e.SubSystemName == "shell")
            {
                Debug.WriteLine("shell REQUESTED");
            }
            else if (e.SubSystemName == "exec")
            {
                Debug.WriteLine("exec REQUESTED");
            }
            else if (e.SubSystemName == "sftp")
            {
                InitializeSftp(e.Channel, e.AttachedUserAuthArgs.Username);
            }

            e.Channel.SendData(Encoding.UTF8.GetBytes($"You ran {e.CommandText}\n"));
            e.Channel.SendClose();
        }

        private void InitializeSftp(SessionChannel channel, string username)
        {
            SftpSubsystem sftpsub = new SftpSubsystem(channel.ClientChannelId);
            channel.DataReceived += (ss, ee) => sftpsub.OnInput(ee);
            sftpsub.OnOutput += (ss, ee) => channel.SendData(ee);

            // Info commands
            sftpsub.onListDir += Sftpsub_onListDir;
            sftpsub.onStat += Sftpsub_onStat;
            sftpsub.onReadData += Sftpsub_onReadData;
            sftpsub.onReadLink += Sftpsub_onReadLink;

            // Edit commands
            sftpsub.onNewFile += Sftpsub_onNewFile;
            sftpsub.onWriteData += Sftpsub_onWriteData;
            sftpsub.onSetStat += Sftpsub_onSetStat; ;
            sftpsub.onMakeDir += Sftpsub_onMakeDir;
            sftpsub.onRemoveDir += Sftpsub_onRemoveDir;
            sftpsub.onRemoveFile += Sftpsub_onRemoveFile;
            sftpsub.onRename += Sftpsub_onRename;
            sftpsub.onMakeLink += Sftpsub_onMakeLink;

            channel.CloseReceived += (ss, ee) =>
            {
                channel.DataReceived -= (ss, ee) => sftpsub.OnInput(ee);
                sftpsub.OnOutput -= (ss, ee) => channel.SendData(ee);
            };
        }

        private void Sftpsub_onMakeLink(object sender, FreeSFtpSharp.Events.SFtpMakeLinkEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (!Fs.Exists(e.Path))
                {
                    var Parent = Fs.GetParentDirectory(e.Path);
                    var SymLink = new NyaFs.Filesystem.Universal.Items.SymLink(e.Path, 0, 0, 0x1ff, e.Target);

                    Parent.Items.Add(SymLink);
                    e.Result = true;
                }
            }
        }

        private void Sftpsub_onReadLink(object sender, FreeSFtpSharp.Events.SFtpReadLinkEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Path))
                {
                    var Element = Fs.GetElement(e.Path);

                    if(Element.ItemType == FilesystemItemType.SymLink)
                    {
                        e.Target = (Element as NyaFs.Filesystem.Universal.Items.SymLink).Target;
                        e.Result = true;
                    }
                }
            }
        }

        private void Sftpsub_onSetStat(object sender, FreeSFtpSharp.Events.SFtpSetStatEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Path))
                {
                    var Element = Fs.GetElement(e.Path);

                    if (e.Mode.HasValue)
                        Element.Mode = e.Mode.Value & 0xfffu;
                    if (e.AccessTime.HasValue)
                        Element.Modified = NyaFs.Filesystem.Universal.Helper.FsHelper.ConvertFromUnixTimestamp(e.AccessTime.Value);
                    if (e.UID.HasValue)
                        Element.User = e.UID.Value;
                    if (e.GID.HasValue)
                        Element.Group = e.GID.Value;

                    e.Result = true;
                }
            }
        }

        private void Sftpsub_onWriteData(object sender, FreeSFtpSharp.Events.SFtpWriteEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Path))
                {
                    var Element = Fs.GetElement(e.Path);

                    if (Element.ItemType == FilesystemItemType.File)
                    {
                        var F = Element as NyaFs.Filesystem.Universal.Items.File;

                        var Content = F.Content;
                        var RequestedSize = e.Offset + e.Data.Length;
                        if(RequestedSize > Content.Length)
                        {
                            var Temp = new byte[RequestedSize];
                            Temp.WriteArray(0, Content, Content.Length);
                            Content = Temp;
                        }
                        Content.WriteArray(e.Offset, e.Data, e.Data.Length);

                        // Update content
                        F.Content = Content;
                        e.Result = true;
                    }
                }
            }
        }

        private void Sftpsub_onNewFile(object sender, FreeSFtpSharp.Events.SFtpNewFileEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Path))
                    e.Result = true;
                else
                {
                    var Parent = Fs.GetParentDirectory(e.Path);

                    var File = new NyaFs.Filesystem.Universal.Items.File(e.Path, 0, 0, 0x1a4, new byte[] { });

                    Parent.Items.Add(File);
                    e.Result = true;
                }
            }
        }

        private void Sftpsub_onReadData(object sender, FreeSFtpSharp.Events.SFtpReadEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Path))
                {
                    var Element = Fs.GetElement(e.Path);

                    if (Element.ItemType == FilesystemItemType.File)
                    {
                        var F = Element as NyaFs.Filesystem.Universal.Items.File;

                        var Content = F.Content;
                        if (e.Offset < Content.Length)
                        {
                            long AvailSize = Content.Length - e.Offset;
                            long Requested = e.Length;

                            if (Requested > AvailSize)
                                Requested = AvailSize;

                            e.Data = Content.ReadArray(e.Offset, Requested);
                            e.Result = true;
                        }
                        else
                        {
                            e.Result = true;
                        }
                    }
                }
            }
        }

        private void Sftpsub_onListDir(object sender, FreeSFtpSharp.Events.SFtpListDirEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Path))
                {
                    var Dir = Fs.GetDirectory(e.Path);

                    var Parent = GetEntry(Dir);
                    Parent.Filename = "..";
                    e.Entries.Add(Parent);
                    foreach (var Element in Dir.Items)
                        e.Entries.Add(GetEntry(Element));

                    e.Result = true;
                }
            }
        }

        private void Sftpsub_onStat(object sender, FreeSFtpSharp.Events.SFtpStatEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Path))
                {
                    var Element = Fs.GetElement(e.Path);

                    e.Entry = GetEntry(Element);
                    e.Result = true;
                }
            }
        }

        private FreeSFtpSharp.Types.SFtpFsEntry GetEntry(NyaFs.Filesystem.Universal.FilesystemItem Item)
        {
            var Entry = new FreeSFtpSharp.Types.SFtpFsEntry();
            Entry.Filename = Item.ShortFilename;
            Entry.UID = Item.User;
            Entry.GID = Item.Group;
            Entry.User = (Item.User == 0) ? "root" : null;
            Entry.Group = (Item.Group == 0) ? "root" : null;
            Entry.Mode = Item.FullMode;
            Entry.Timestamp = Item.Modified;
            Entry.Size = Item.Size;
            Entry.Type = ConvertType(Item.ItemType);

            return Entry;
        }

        private FreeSFtpSharp.Types.SFtpFsEntryType ConvertType(FilesystemItemType itemType)
        {
            switch(itemType)
            {
                case FilesystemItemType.Unknown: return FreeSFtpSharp.Types.SFtpFsEntryType.Unknown;
                case FilesystemItemType.File: return FreeSFtpSharp.Types.SFtpFsEntryType.File;
                case FilesystemItemType.Directory: return FreeSFtpSharp.Types.SFtpFsEntryType.Directory;
                case FilesystemItemType.SymLink: return FreeSFtpSharp.Types.SFtpFsEntryType.SymLink;
                case FilesystemItemType.Character: return FreeSFtpSharp.Types.SFtpFsEntryType.Character;
                case FilesystemItemType.Block: return FreeSFtpSharp.Types.SFtpFsEntryType.Block;
                case FilesystemItemType.Fifo: return FreeSFtpSharp.Types.SFtpFsEntryType.Fifo;
                case FilesystemItemType.Socket: return FreeSFtpSharp.Types.SFtpFsEntryType.Socket;
                default: return FreeSFtpSharp.Types.SFtpFsEntryType.Unknown;
            }
        }

        private void Sftpsub_onRename(object sender, FreeSFtpSharp.Events.SFtpRenameEventArgs e)
        {
            Console.WriteLine($"Sftpsub_onRename: {e.OldPath} => {e.NewPath}");
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.OldPath))
                {
                    var Element = Fs.GetElement(e.OldPath);
                    var OldParent = Fs.GetParentDirectory(e.OldPath);
                    var NewParent = Fs.GetParentDirectory(e.NewPath);

                    if (OldParent != NewParent)
                    {
                        NewParent.Items.Add(Element);
                        OldParent.Items.Remove(Element);
                    }
                    Element.Filename = e.NewPath;

                    e.Result = true;
                }
            }
        }


        private void Sftpsub_onRemoveFile(object sender, FreeSFtpSharp.Events.SFtpRemoveFileEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Filename))
                {
                    Fs.Delete(e.Filename);
                    e.Result = true;
                }
            }
        }

        private void Sftpsub_onRemoveDir(object sender, FreeSFtpSharp.Events.SFtpRemoveDirEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (Fs.Exists(e.Path))
                {
                    var Element = Fs.GetDirectory(e.Path);

                    if(Element.ItemType == FilesystemItemType.Directory)
                    {
                        Fs.Delete(e.Path);
                        e.Result = true;
                    }
                }
            }
        }

        private void Sftpsub_onMakeDir(object sender, FreeSFtpSharp.Events.SFtpMakeDirEventArgs e)
        {
            var Fs = Processor.GetFs();

            if (Fs != null)
            {
                if (!Fs.Exists(e.Path))
                {
                    var Parent = Fs.GetParentDirectory(e.Path);
                    var Dir = new NyaFs.Filesystem.Universal.Items.Dir(e.Path, 0, 0, 0x1ed);

                    Parent.Items.Add(Dir);
                    e.Result = true;
                }
            }
        }
    }
}

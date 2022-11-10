using Extension.Array;
using FxSsh;
using FxSsh.Messages.Connection;
using FxSsh.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FreeSFtpSharp
{
    // https://github.com/mikaelliljedahl/freesftpsharp/
    public class SftpSubsystem
    {
        public event Events.SFtpRenameEventHandler onRename;
        public event Events.SFtpRemoveDirEventHandler onRemoveDir;
        public event Events.SFtpMakeDirEventHandler onMakeDir;
        public event Events.SFtpRemoveFileEventHandler onRemoveFile;
        public event Events.SFtpStatEventHandler onStat;
        public event Events.SFtpSetStatEventHandler onSetStat;
        public event Events.SFtpListDirEventHandler onListDir;
        public event Events.SFtpReadEventHandler onReadData;
        public event Events.SFtpWriteEventHandler onWriteData;
        public event Events.SFtpNewFileEventHandler onNewFile;
        public event Events.SFtpReadLinkEventHandler onReadLink;
        public event Events.SFtpMakeLinkEventHandler onMakeLink;

        private string ActivePath = "/";

        private uint channel;
        internal EventHandler<ICollection<byte>> OnOutput;

        Dictionary<string, string> HandleToPathDictionary;
        Dictionary<string, List<Types.SFtpFsEntry>> HandleToPathDirList;
        Dictionary<string, string> HandleToFileStreamDictionary;

        public SftpSubsystem(uint channel)
        {
            this.channel = channel;

            HandleToPathDictionary = new Dictionary<string, string>();
            HandleToFileStreamDictionary = new Dictionary<string, string>();
            HandleToPathDirList = new Dictionary<string, List<Types.SFtpFsEntry>>();
        }

        /// <summary>
        /// Data to store incomplete packets
        /// </summary>

        List<byte> Pending = new List<byte>();

        /// <summary>
        /// It first reads the first 5 bytes of the input, which is the length of the message and the message
        /// type. Then it reads the rest of the message and calls the appropriate function to handle the
        /// message based on the type
        /// </summary>
        /// <param name="ee">the byte array of the data received from the client</param>
        internal void OnInput(byte[] ee)
        {
            Pending.AddRange(ee);
            while(Pending.Count > 0)
            {
                var input = Pending.ToArray();

                if (input.Length >= 5)
                {
                    //uint32 length
                    //byte type
                    //byte[length - 1] data payload
                    var msglength = input.ReadUInt32BE(0);
                    var msgtype = (RequestPacketType)input.ReadByte(4);

                    //Console.WriteLine($"{msgtype} {msglength}  (avail {input.Length})");
                    if (msglength > input.Length - 4)
                        return;

                    // Build a packet
                    byte[] packet = input.ReadArray(5, msglength - 1);
                    var packetreader = new SshDataWorker(packet);

                    Pending.Clear();
                    var availdata = input.Length - msglength - 4;
                    if (availdata > 0)
                        Pending.AddRange(input.ReadArray(input.Length - availdata, availdata));

                    switch (msgtype)
                    {
                        case RequestPacketType.SSH_FXP_INIT:
                            HandleInit(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_REALPATH:
                            HandleRealPath(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_READDIR:
                            HandleReadDir(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_OPENDIR:
                            HandleOpenDir(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_SETSTAT:
                            HandleSetStat(packetreader, true);
                            break;

                        case RequestPacketType.SSH_FXP_STAT: // follows symbolic links
                            HandleStat(packetreader, true);
                            break;

                        case RequestPacketType.SSH_FXP_LSTAT: // does not follow symbolic links
                            HandleStat(packetreader, false);
                            break;

                        case RequestPacketType.SSH_FXP_FSTAT: // SSH_FXP_FSTAT differs from the others in that it returns status information for an open file(identified by the file handle).
                            HandleFStat(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_SYMLINK:
                            HandleLink(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_READLINK:
                            HandleReadLink(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_CLOSE:
                            HandleClose(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_OPEN:
                            HandleFileOpen(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_READ:
                            HandleReadFile(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_WRITE:
                            HandleWriteFile(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_REMOVE:
                            HandleRemoveFile(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_MKDIR:
                            HandleMakeDir(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_RMDIR:
                            HandleRemoveDir(packetreader);
                            break;

                        case RequestPacketType.SSH_FXP_RENAME:
                            HandleRename(packetreader);
                            break;

                        default:
                            // unsupported command
                            uint requestId = packetreader.ReadUInt32();
                            SendStatus(requestId, SftpStatusType.SSH_FX_OP_UNSUPPORTED);
                            break;
                    }
                }
                else
                    break;
            }
        }

        private string GetAbsolutePath(string Path)
        {
            return System.IO.Path.Combine(ActivePath, Path);
        }

        private void SetAbsolutePath(string Path)
        {
            ActivePath = Path;
        }

        private void HandleRename(SshDataWorker reader)
        {
            uint requestId = reader.ReadUInt32();

            var oldpath = GetAbsolutePath(reader.ReadString(Encoding.UTF8));
            var newpath = GetAbsolutePath(reader.ReadString(Encoding.UTF8));

            if (oldpath.Contains(".."))
            {
                SendStatus(requestId, SftpStatusType.SSH_FX_PERMISSION_DENIED);
            }
            else
            {
                var e =  new Events.SFtpRenameEventArgs(oldpath, newpath);
                onRename?.Invoke(this, e);

                if(e.Result)
                    SendStatus(requestId, SftpStatusType.SSH_FX_OK);
                else
                    SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
            }
        }

        private void HandleRemoveDir(SshDataWorker reader)
        {
            uint requestId = reader.ReadUInt32();
            var path = GetAbsolutePath(reader.ReadString(Encoding.UTF8));

            if (path.Contains(".."))
            {
                SendStatus(requestId, SftpStatusType.SSH_FX_PERMISSION_DENIED);
            }
            else
            {
                var le = new Events.SFtpListDirEventArgs(path);
                onListDir?.Invoke(this, le);

                if (le.Result)
                {
                    if (le.Entries.Count > 0)
                        SendStatus(requestId, SftpStatusType.SSH_FX_DIR_NOT_EMPTY);
                    else
                    {
                        var e = new Events.SFtpRemoveDirEventArgs(path);
                        onRemoveDir?.Invoke(this, e);

                        if (e.Result)
                            SendStatus(requestId, SftpStatusType.SSH_FX_OK);
                        else
                            SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
                    }
                }
                else
                    SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
            }
        }

        private void HandleMakeDir(SshDataWorker reader)
        {
            // New directories can be created using the SSH_FXP_MKDIR request.It has the following format:
            // uint32 id
            // string path
            // ATTRS attrs
            uint requestId = reader.ReadUInt32();
            var path = GetAbsolutePath(reader.ReadString(Encoding.UTF8));

            //uint32 flags
            var flags = reader.ReadUInt32();

            if (path.Contains(".."))
            {
                SendStatus(requestId, SftpStatusType.SSH_FX_PERMISSION_DENIED);
            }
            else
            {
                var e = new Events.SFtpMakeDirEventArgs(path);
                onMakeDir?.Invoke(this, e);

                if (e.Result)
                    SendStatus(requestId, SftpStatusType.SSH_FX_OK);
                else
                    SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
            }
        }

        private void HandleRemoveFile(SshDataWorker reader)
        {
            uint requestId = reader.ReadUInt32();
            var filename = GetAbsolutePath(reader.ReadString(Encoding.UTF8));

            var e = new Events.SFtpRemoveFileEventArgs(filename);
            onRemoveFile?.Invoke(this, e);

            if (e.Result)
                SendStatus(requestId, SftpStatusType.SSH_FX_OK);
            else
                SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
        }

        private void HandleSetStat(SshDataWorker reader, bool FollowSymlink)
        {
            uint requestId = reader.ReadUInt32();
            var srcpath = reader.ReadString(Encoding.UTF8);
            var processedpath = ProcessPath(srcpath);
            var path = GetAbsolutePath(processedpath);

            var e = new Events.SFtpSetStatEventArgs(path);
            var flags = reader.ReadUInt32(); // TODO: check flags
            if ((flags & 0x0002) != 0)
            {
                e.UID = reader.ReadUInt32();
                e.GID = reader.ReadUInt32();
            }
            if ((flags & 0x0004) != 0)
            {
                e.Mode = reader.ReadUInt32();
            }
            if ((flags & 0x0008) != 0)
            {
                var atime = reader.ReadUInt32();
                e.AccessTime = reader.ReadUInt32();
            }

            onSetStat?.Invoke(this, e);

            if (e.Result)
                SendStatus(requestId, SftpStatusType.SSH_FX_OK);
            else
                SendStatus(requestId, SftpStatusType.SSH_FX_NO_SUCH_PATH);
        }

        private void HandleStat(SshDataWorker reader, bool FollowSymlink)
        {
            uint requestId = reader.ReadUInt32();
            var srcpath = reader.ReadString(Encoding.UTF8);
            var processedpath = ProcessPath(srcpath);
            var path = GetAbsolutePath(processedpath);

            var e = new Events.SFtpStatEventArgs(path, FollowSymlink);
            onStat?.Invoke(this, e);

            if (e.Result)
                SendAttributes(requestId, e.Entry);
            else
                SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
        }

        private void HandleFStat(SshDataWorker reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.UTF8);
            if (HandleToPathDictionary.ContainsKey(handle))
            {
                var path = HandleToPathDictionary[handle];

                var e = new Events.SFtpStatEventArgs(path, true);
                onStat?.Invoke(this, e);
                if(e.Result)
                    SendAttributes(requestId, e.Entry);
                else
                    SendStatus(requestId, SftpStatusType.SSH_FX_NO_SUCH_FILE);
            }
            else
                SendStatus(requestId, SftpStatusType.SSH_FX_NO_SUCH_FILE);
        }

        private void HandleClose(SshDataWorker reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.UTF8);

            if (HandleToPathDictionary.ContainsKey(handle))
            {
                HandleToPathDictionary.Remove(handle);
                if (HandleToFileStreamDictionary.ContainsKey(handle))
                {
                    // TODO: Close...
                    
                    HandleToFileStreamDictionary.Remove(handle);
                }
            }

            SendStatus(requestId, SftpStatusType.SSH_FX_OK);
        }

        private void HandleLink(SshDataWorker reader)
        {
            uint requestId = reader.ReadUInt32();
            var srcpath = reader.ReadString(Encoding.UTF8);
            var processedpath = ProcessPath(srcpath);
            var path = GetAbsolutePath(processedpath);
            var target = reader.ReadString(Encoding.UTF8);

            var e = new Events.SFtpMakeLinkEventArgs(path, target);
            onMakeLink?.Invoke(this, e);

            if (e.Result)
                SendStatus(requestId, SftpStatusType.SSH_FX_OK);
            else
                SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
        }

        private void HandleReadLink(SshDataWorker reader)
        {
            uint requestId = reader.ReadUInt32();
            var srcpath = reader.ReadString(Encoding.UTF8);
            var processedpath = ProcessPath(srcpath);
            var path = GetAbsolutePath(processedpath);

            var e = new Events.SFtpReadLinkEventArgs(path);
            onReadLink?.Invoke(this, e);

            if (e.Result)
            {
                SshDataWorker writer = new SshDataWorker();
                writer.Write((byte)RequestPacketType.SSH_FXP_NAME);
                writer.Write((uint)requestId);
                writer.Write((uint)1); // one file/directory at a time

                writer.Write(e.Target, Encoding.UTF8);
                writer.Write(e.Target, Encoding.UTF8);

                SendPacket(writer.ToByteArray());
            }
            else
                SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
        }

        /// <summary>
        /// The function reads the request-id, handle, offset and length from the packet and then reads
        /// the file from the offset and length specified 
        /// </summary>
        /// <param name="SshDataWorker">This is a class that I wrote to help with reading and writing SSH
        /// packets. </param>
        /// <returns>
        /// SendStatus method will be called on EOF or on error otherwise the method will will send data from 
        /// the file being read to the SSH channel
        /// </returns>
        private void HandleReadFile(SshDataWorker reader)
        {
            SshDataWorker writer = new SshDataWorker();
            var requestId = reader.ReadUInt32(); //uint32 request-id
            var handle = reader.ReadString(Encoding.UTF8);
            if (HandleToPathDictionary.ContainsKey(handle))
            {
                var fs = HandleToFileStreamDictionary[handle];
                var offset = (long)reader.ReadUInt64();
                var length = (int)reader.ReadUInt32();

                var e = new Events.SFtpReadEventArgs(fs, offset, length);
                onReadData?.Invoke(this, e);

                if(e.Result)
                {
                    if (e.Data != null)
                    {
                        writer.Write((byte)RequestPacketType.SSH_FXP_DATA);
                        writer.Write((uint)requestId);
                        writer.WriteBinary(e.Data);
                        SendPacket(writer.ToByteArray());
                    }
                    else
                        SendStatus((uint)requestId, SftpStatusType.SSH_FX_EOF);
                }
                else
                    SendStatus((uint)requestId, SftpStatusType.SSH_FX_NO_SUCH_FILE);
            }
            else
            {
                SendStatus((uint)requestId, SftpStatusType.SSH_FX_OP_UNSUPPORTED);
                return;
                // send invalid handle: SSH_FX_INVALID_HANDLE
            }
        }
 
        /// <summary>
        /// It reads the file handle, the offset from the beginning of the file, and the data to write,
        /// and then writes the data to the file at the specified offset
        /// </summary>
        /// <param name="SshDataWorker">This is a class that I wrote to help me read and write data to
        /// the SSH stream.</param>
        /// <returns>
        /// The method will return status to the SSH client using the SendStatus method.
        /// </returns>        
        private void HandleWriteFile(SshDataWorker reader)
        {
            SshDataWorker writer = new SshDataWorker();
            var requestId = reader.ReadUInt32(); //uint32 request-id
            var handle = reader.ReadString(Encoding.UTF8);
            if (HandleToPathDictionary.ContainsKey(handle))
            {
                try
                {
                    var offset = (long)reader.ReadUInt64();
                    var path = HandleToPathDictionary[handle];
                    var buffer = reader.ReadBinary();

                    var e = new Events.SFtpWriteEventArgs(path, offset, buffer);
                    onWriteData?.Invoke(this, e);

                    if (e.Result)
                        SendStatus(requestId, SftpStatusType.SSH_FX_OK);
                    else
                        SendStatus(requestId, SftpStatusType.SSH_FX_OP_UNSUPPORTED);
                }
                catch(Exception E)
                {
                    SendStatus(requestId, SftpStatusType.SSH_FX_BAD_MESSAGE);
                }
            }
            else
            {
                SendStatus((uint)requestId, SftpStatusType.SSH_FX_INVALID_HANDLE);
                return;
            }
        }

        private void HandleOpenDir(SshDataWorker reader)
        {
            var requestId = reader.ReadUInt32();
            var path = reader.ReadString(Encoding.UTF8);
            var handle = GenerateHandle();

            path = ProcessPath(path);
            path = GetAbsolutePath(path);

            var e = new Events.SFtpListDirEventArgs(path);
            onListDir?.Invoke(this, e);

            if (e.Result)
            {
                var writer = new SshDataWorker();

                HandleToPathDictionary.Add(handle, path);
                HandleToPathDirList.Add(handle, e.Entries);

                writer.Write((byte)RequestPacketType.SSH_FXP_HANDLE);
                writer.Write((uint)requestId);
                writer.Write(handle, Encoding.UTF8);

                SendPacket(writer.ToByteArray());
            }
            else
                SendStatus(requestId, SftpStatusType.SSH_FX_FAILURE);
        }

        private string ProcessPath(string Path)
        {
            var Parts = Path.Split(new char[] { '/' });

            var Res = new Stack<string>();
            foreach (var P in Parts)
            {
                if (P.Length > 0)
                {
                    if (P == "..")
                        Res.Pop();
                    else
                        Res.Push(P);
                }
            }

            return "/" + String.Join("/", Res.Reverse().ToArray());
        }

        private static string GenerateHandle()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        private void HandleFileOpen(SshDataWorker reader)
        {
            SshDataWorker writer = new SshDataWorker();

            var requestId = reader.ReadUInt32(); //uint32 request-id
            var path = reader.ReadString(Encoding.UTF8); // //string filename [UTF-8]
            path = GetAbsolutePath(path);
            string handle = GenerateHandle();
            HandleToPathDictionary.Add(handle, path);

            //ATTRS  attrs
            //uint32 desired-access
            var desired_access = reader.ReadUInt32();
            //uint32 flags
            var flags = reader.ReadUInt32();

            var write = desired_access & (uint)FileSystemOperation.Write;
            var read = desired_access & (uint)FileSystemOperation.Read;
            var create = desired_access & (uint)FileSystemOperation.Create;

            if (read > 0)
            {
                var e = new Events.SFtpStatEventArgs(path, true);
                onStat?.Invoke(this, e);

                if (e.Result)
                {
                    HandleToFileStreamDictionary.Add(handle, path);

                    writer.Write((byte)RequestPacketType.SSH_FXP_HANDLE);
                    writer.Write((uint)requestId);
                    writer.Write(handle, Encoding.UTF8);
                    // returns SSH_FXP_HANDLE on success or a SSH_FXP_STATUS message on fail

                    SendPacket(writer.ToByteArray());
                }
                else
                    SendStatus(requestId, SftpStatusType.SSH_FX_NO_SUCH_FILE);
            }
            else if (write > 0)
            {
                // Файл может быть, а, может и не быть
                var e = new Events.SFtpStatEventArgs(path, true);
                onStat?.Invoke(this, e);

                if (e.Result)
                {
                    // Файл есть, переписываем...
                    HandleToFileStreamDictionary.Add(handle, path);

                    writer.Write((byte)RequestPacketType.SSH_FXP_HANDLE);
                    writer.Write((uint)requestId);
                    writer.Write(handle, Encoding.UTF8);
                    // returns SSH_FXP_HANDLE on success or a SSH_FXP_STATUS message on fail

                    SendPacket(writer.ToByteArray());
                }
                else
                {
                    // Файла нет. Создадим...
                    var ne = new Events.SFtpNewFileEventArgs(path);
                    onNewFile?.Invoke(this, ne);
                    if (ne.Result)
                    {
                        HandleToFileStreamDictionary.Add(handle, path);

                        writer.Write((byte)RequestPacketType.SSH_FXP_HANDLE);
                        writer.Write((uint)requestId);
                        writer.Write(handle, Encoding.UTF8);
                        // returns SSH_FXP_HANDLE on success or a SSH_FXP_STATUS message on fail

                        SendPacket(writer.ToByteArray());
                    }
                    else
                        SendStatus(requestId, SftpStatusType.SSH_FX_NO_SUCH_PATH);
                }
            }
        }

        private string HandleReadDir(SshDataWorker reader)
        {
            var requestId = reader.ReadUInt32();
            var handle = reader.ReadString(Encoding.UTF8);
            var writer = new SshDataWorker();

            if (HandleToPathDictionary.ContainsKey(handle)) // remove after handle is used first time
            {
                var filesanddirs  = HandleToPathDirList[handle];

                var entry = filesanddirs.FirstOrDefault();
                if (entry != null)
                {
                    ReturnReadDir(entry, requestId);
                    filesanddirs.Remove(entry);
                }
                else
                {
                    HandleToPathDictionary.Remove(handle); // remove will return EOF next time
                    SendStatus(requestId, SftpStatusType.SSH_FX_EOF);
                }
            }
            else
            {
                // return SSH_FXP_STATUS indicating SSH_FX_EOF  when all files have been listed
                SendStatus(requestId, SftpStatusType.SSH_FX_EOF);
            }

            return handle;
        }

        private void ReturnReadDir(Types.SFtpFsEntry entry, uint requestId)
        {
            var writer = new SshDataWorker();

            // returns SSH_FXP_NAME or SSH_FXP_STATUS with SSH_FX_EOF 
            writer.Write((byte)RequestPacketType.SSH_FXP_NAME);
            writer.Write((uint)requestId);
            writer.Write((uint)1); // one file/directory at a time

            writer.Write(entry.Filename, Encoding.UTF8);
            writer.Write(entry.LsLine, Encoding.UTF8);
            writer.Write(GetAttributes(entry));
            
            SendPacket(writer.ToByteArray());
        }

        private void HandleRealPath(SshDataWorker reader)
        {
            var requestId = reader.ReadUInt32();
            var path = reader.ReadString(Encoding.UTF8);

            // return current dir for absolutepath
            if (path == "." || path == "/.")
                path = "/"; // replace with current filepath

            path = ProcessPath(path);
            path = GetAbsolutePath(path);

            var e = new Events.SFtpStatEventArgs(path, false);
            onStat?.Invoke(this, e);

            if(e.Result)
            {
                SshDataWorker writer = new SshDataWorker();

                writer.Write((byte)RequestPacketType.SSH_FXP_NAME);
                writer.Write((uint)requestId);
                writer.Write((uint)1); // always count = 1 for realpath

                writer.Write(path, Encoding.UTF8);
                writer.Write(path, Encoding.UTF8);

                SendPacket(writer.ToByteArray());
            }
            else
                SendStatus(requestId, SftpStatusType.SSH_FX_NO_SUCH_PATH);
        }

        private void HandleInit(SshDataWorker reader)
        {
            SshDataWorker writer = new SshDataWorker();
            var sftpclientversion = reader.ReadUInt32();
            writer.Write((byte)RequestPacketType.SSH_FXP_VERSION);
            var version = Math.Min(3, sftpclientversion);
            writer.Write((uint)version); // SFTP protocol version
            SendPacket(writer.ToByteArray());
        }

        private void SendAttributes(uint requestId, Types.SFtpFsEntry entry)
        {
            SshDataWorker writer = new SshDataWorker();
            writer.Write((byte)RequestPacketType.SSH_FXP_ATTRS);
            writer.Write(requestId);
            writer.Write(GetAttributes(entry));
            SendPacket(writer.ToByteArray());
        }

        private byte[] GetAttributes(Types.SFtpFsEntry entry)
        {
            SshDataWorker writer = new SshDataWorker();

            // Todo: rawpacket
            writer.Write(0x0F000005u); // flags
            writer.Write((ulong)entry.Size); // size
            writer.Write((uint)entry.UID); // uid
            writer.Write((uint)entry.GID); // gid
            writer.Write((uint)entry.Mode); // permissions
            writer.Write((uint)GetUnixFileTime(entry.Timestamp)); //atime   
            writer.Write((uint)GetUnixFileTime(entry.Timestamp)); //mtime

            return writer.ToByteArray();
        }

        private void SendStatus(uint requestId, SftpStatusType status)
        {
            SshDataWorker writer = new SshDataWorker();
            writer.Write((byte)RequestPacketType.SSH_FXP_STATUS);
            writer.Write(requestId);
            writer.Write((uint)status); // status code
                                        //writer.Write("", Encoding.UTF8);
                                        //writer.Write("", Encoding.UTF8);
            SendPacket(writer.ToByteArray());
        }

        /// <summary>
        /// They are represented as seconds from Jan 1, 1970 in UTC.
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        private uint GetUnixFileTime(DateTime time)
        {
            TimeSpan diff = time.ToUniversalTime() - DateTime.UnixEpoch;
            return (uint)Math.Floor(diff.TotalSeconds);

        }

        /// <summary>
        /// calculates and add packet length uint to the beginning of the packet
        /// </summary>
        /// <param name="data"></param>
        internal void SendPacket(byte[] data)
        {
            var packettosend = data.ToList();
            var length = packettosend.Count();
            SshDataWorker packetlengthwriter = new SshDataWorker();
            packetlengthwriter.Write((uint)length);
            packettosend.InsertRange(0, packetlengthwriter.ToByteArray());

            if (OnOutput != null)
                OnOutput(this, packettosend);
        }

        internal void Send(byte[] data)
        {

            if (OnOutput != null)
                OnOutput(this, data);
        }
    }
}

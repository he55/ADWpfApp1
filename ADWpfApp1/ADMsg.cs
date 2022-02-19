﻿using System;
using System.IO;
using System.Net;
using System.Text;

namespace ADWpfApp1
{
    public enum ADMsgType : int
    {
        hello = 0x1,
        helloOK,
        sendFile,
        sendFileOK,
        sendUrl,
        sendString
    }

    public struct ADMsg
    {
        const uint HEADER = 0xad00ad00;

        uint header;
        public ADMsgType type;
        int len;
        byte[] data;

        public ADMsg(ADMsgType type)
        {
            this.header = HEADER;
            this.type = type;
            this.len = 0;
            this.data = new byte[0];
        }

        public ADMsg sendFileOKData(IPEndPoint ep)
        {
            byte[] addr = ep.Address.GetAddressBytes();
            this.len = 12;
            this.data = new byte[12] {
                addr[0],
                addr[1],
                addr[2],
                addr[3],
                0,
                0,
                0,
                0,
                (byte)ep.Port,
                (byte)(ep.Port>>8),
                (byte)(ep.Port>>16),
                (byte)(ep.Port>>24),
            };

            return this;
        }

        public ADMsg sendStringData(string str)
        {
            this.data = Encoding.UTF8.GetBytes(str);
            this.len = this.data.Length;
            return this;
        }

        public ADMsg AddUrlData(string url)
        {
           return sendStringData(url);
        }

        public ADMsg helloData()
        {
            return this;
        }

        public ADMsg helloOKData(string name)
        {
          return  sendStringData(name);
        }

        public ADMsg sendFileData(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            long length = fileInfo.Length;

            byte[] vs1 = BitConverter.GetBytes(length);
            byte[] vs = Encoding.UTF8.GetBytes(fileInfo.Name);

            this.len = 8 + vs.Length;
            this.data = new byte[this.len];

            vs1.CopyTo(this.data, 0);
            vs.CopyTo(this.data, 8);

            return this;
        }

        public IPEndPoint ToIPData()
        {
            long addr = BitConverter.ToInt64(this.data, 0);
            int port = BitConverter.ToInt32(this.data, 8);
            return new IPEndPoint(addr, port);
        }

        public string ToStringData()
        {
            return Encoding.UTF8.GetString(this.data);
        }

        public string ToUrlData()
        {
            return ToStringData();
        }

        public string ToNameData()
        {
            return ToStringData();
        }

        public MyDownloadFileInfo ToFileData()
        {
            MyDownloadFileInfo myDownloadFileInfo = new MyDownloadFileInfo();
            myDownloadFileInfo.Len = BitConverter.ToInt64(this.data, 0);
            myDownloadFileInfo.FileName = Encoding.UTF8.GetString(this.data, 8, this.len - 8);
            return myDownloadFileInfo;
        }

        public byte[] ToArr()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(this.header);
                    binaryWriter.Write((int)this.type);
                    binaryWriter.Write(this.len);
                    binaryWriter.Write(this.data);
                }
                return memoryStream.ToArray();
            }
        }

        public static ADMsg ToMSG(byte[] buf)
        {
            uint v = BitConverter.ToUInt32(buf, 0);
            if (v != HEADER)
            {
                throw new Exception();
            }

            ADMsg msg;
            msg.header = HEADER;
            msg.type = (ADMsgType)BitConverter.ToInt32(buf, 4);
            msg.len = BitConverter.ToInt32(buf, 8);
            msg.data = new byte[0];

            if (msg.len != 0)
            {
                msg.data = new byte[msg.len];
                Array.Copy(buf, 12, msg.data, 0, msg.len);
            }

            return msg;
        }

        public static bool IsMSG(byte[] buf)
        {
            uint v = BitConverter.ToUInt32(buf, 0);
            return v == HEADER;
        }
    }
}

﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ADWinFormsApp1
{
    public class ServerTcp
    {
        const int BufferSize = 1024;
        static string path = @"D:\";

        public static void StartServerTcp(IPEndPoint remoteEP)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(remoteEP);
            sock.Listen(1);
            Console.WriteLine("Begin listen...");

            Task.Run(() =>
            {
                while (true)
                {
                    Socket client = sock.Accept();
                    if (client.Connected)
                    {
                        Thread cThread = new Thread(new ParameterizedThreadStart(myClient));
                        cThread.IsBackground = true;
                        cThread.Start(client);
                    }
                }
            });
        }

        static void myClient(object oSocket)
        {
            Socket handler = (Socket)oSocket;
            string clientName = handler.RemoteEndPoint.ToString();
            Console.WriteLine("新来一个客户:" + clientName);

            try
            {
                byte[] buffer = new byte[BufferSize];
                int count = handler.Receive(buffer);

                Console.WriteLine("收到" + clientName + ":" + Encoding.Default.GetString(buffer, 0, count));
                string[] command = Encoding.Default.GetString(buffer, 0, count).Split(',');

                if (command[0] == "namelength")
                {
                    string fileName = command[1];
                    long length = Convert.ToInt64(command[2]);
                    handler.Send(Encoding.Default.GetBytes("OK"));

                    Console.WriteLine("Receiveing file:" + fileName + ".Plz wait...");

                    string path1 = Path.Combine(path, fileName);
                    using (FileStream writer = new FileStream(path1, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        long receive = 0L;
                        int received;
                        while (receive < length)
                        {
                            received = handler.Receive(buffer);
                            writer.Write(buffer, 0, received);
                            writer.Flush();
                            receive += received;
                        }
                    }

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                    Console.WriteLine("Receive finish.\n");
                }
            }
            catch
            {
                Console.WriteLine("客户:" + clientName + "退出");
            }
        }
    }
}

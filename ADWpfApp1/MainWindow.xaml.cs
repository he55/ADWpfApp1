using ModernWpf;
using ModernWpf.Controls;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using IDataObject_Com = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace ADWpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int PORT = 12500;

        Socket socket1;
        readonly IPEndPoint BroadcastEP = new IPEndPoint(IPAddress.Broadcast, PORT);
        bool isInitServer;
        IPAddress ipAddress;
        IPEndPoint selectEP;
        string filePath;
        int selectedIndex;

        public ObservableCollection<UserInfo> Devices { get; set; } = new ObservableCollection<UserInfo>();
        public UserInfo UserInfo { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Devices.Add(new UserInfo { Name = "qwe", IPString = "ip" });
            Devices.Add(new UserInfo { Name = "asd", IPString = "cp" });
            Devices.Add(new UserInfo { Name = "zxc", IPString = "hp", IsSel = true });
            Devices.Add(new UserInfo { Name = "rty", IPString = "up" });
            Devices.Add(new UserInfo { Name = "fgh", IPString = "dp" });
            Devices.Add(new UserInfo { Name = "vbn", IPString = "yp" });
            UserInfo = Devices[0];
            this.grid1.DataContext = UserInfo;

            this.DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ipAddress = GetIPAddr();

            socket1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket1.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);
            socket1.Bind(localEndPoint);

            Task.Run(() => { OnReceive(); });
        }

        static IPAddress GetIPAddr()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var item in ipHostInfo.AddressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                {
                    return item;
                }
            }
            return null;
        }

        void OnReceive()
        {
            byte[] buf = new byte[1024];

            while (true)
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                int len = socket1.ReceiveFrom(buf, ref ep);
                if (len >= 8 && ADMsg.IsMSG(buf))
                {
                    ADMsg msg = ADMsg.ToMSG(buf);
                    ADMsgType msgType = (ADMsgType)msg.GetMsgType();

                    if (msgType == ADMsgType.hello)
                    {
                        string v = Dns.GetHostName();
                        byte[] buf2 = ADMsg.helloOKData(v).ToArr();
                        socket1.SendTo(buf2, new IPEndPoint(((IPEndPoint)ep).Address, PORT));
                    }
                    else if (msgType == ADMsgType.helloOK)
                    {
                        UserInfo userInfo = new UserInfo();
                        userInfo.Name = msg.ToStringData();
                        userInfo.IP = ((IPEndPoint)ep).Address.Address;
                        userInfo.IPString = ((IPEndPoint)ep).Address.ToString();

                        this.Dispatcher.Invoke(() =>
                        {
                            Devices.Add(userInfo);
                        });
                    }
                    else if (msgType == ADMsgType.sendFile)
                    {
                        MyDownloadFileInfo.DownloadFileInfo = msg.ToFileData();

                        this.Dispatcher.Invoke(async () =>
                        {
                            this.Activate();

                            ContentDialogExample dialog = new ContentDialogExample();
                            ContentDialogResult result = await dialog.ShowAsync();
                            if (result == ContentDialogResult.Primary)
                            {
                                IPEndPoint remoteEP = new IPEndPoint(ipAddress, PORT);
                                if (!isInitServer)
                                {
                                    isInitServer = true;
                                    TcpServer.StartServerTcp(remoteEP);
                                }

                                byte[] buf2 = ADMsg.sendFileOKData(remoteEP).ToArr();
                                socket1.SendTo(buf2, new IPEndPoint(((IPEndPoint)ep).Address, PORT));
                            }
                            else
                            {
                                byte[] buf2 = ADMsg.sendFileCancelData().ToArr();
                                socket1.SendTo(buf2, new IPEndPoint(((IPEndPoint)ep).Address, PORT));
                            }
                        });
                    }
                    else if (msgType == ADMsgType.sendFileOK)
                    {
                        IPEndPoint remoteEP = msg.ToIPData();
                        TcpServer.StartClientTcp(filePath, remoteEP);
                    }
                    else if (msgType == ADMsgType.sendFileCancel)
                    {
                    }
                    else if (msgType == ADMsgType.sendUrl)
                    {
                        string v = msg.ToStringData();
                    }
                    else if (msgType == ADMsgType.sendString)
                    {
                        string v = msg.ToStringData();
                    }

                    Debug.WriteLine($"{ep} => {msgType} : {msg.ToStringData()}");
                }
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Devices.Clear();

            byte[] buf = ADMsg.helloData().ToArr();
            socket1.SendTo(buf, BroadcastEP);
        }

        void DataActionMet(bool isfile, string str)
        {
            selectEP = new IPEndPoint(Devices[selectedIndex].IP, PORT);
            if (isfile)
            {
                filePath = str;

                byte[] buf = ADMsg.sendFileData(str).ToArr();
                socket1.SendTo(buf, selectEP);
            }
            else
            {
                byte[] buf = ADMsg.sendStringData(str).ToArr();
                socket1.SendTo(buf, selectEP);
            }
        }


        private void ToggleTheme(object sender, RoutedEventArgs e)
        {
            if (ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            }
            else
            {
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            }
        }


        #region DragDropHelper

        private IDropTargetHelper ddHelper = (IDropTargetHelper)new DragDropHelper();

        Win32Point GetWin32Point(DragEventArgs e)
        {
            Point p = this.PointToScreen(e.GetPosition(this));
            Win32Point wp;
            wp.x = (int)p.X;
            wp.y = (int)p.Y;
            return wp;
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;

            Win32Point wp = GetWin32Point(e);
            ddHelper.DragEnter(new WindowInteropHelper(this).Handle, e.Data as IDataObject_Com, ref wp, (int)e.Effects);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            Win32Point wp = GetWin32Point(e);
            ddHelper.DragOver(ref wp, (int)e.Effects);

            Point point = e.GetPosition(listBox1);
            int v = MyHelper.GetIndexAtPoint(listBox1, point);

            if (selectedIndex != -1)
                Devices[selectedIndex].IsSel = false;

            if (v != -1)
                Devices[v].IsSel = true;

            selectedIndex = v;

            Debug.WriteLine($"sel: {selectedIndex} {point}");
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            ddHelper.DragLeave();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            Win32Point wp = GetWin32Point(e);
            ddHelper.Drop(e.Data as IDataObject_Com, ref wp, (int)e.Effects);


            if (selectedIndex != -1)
            {
                Devices[selectedIndex].IsSel = false;

                DataObject data = (DataObject)e.Data;
                string v = data.GetText();
                if (!string.IsNullOrEmpty(v))
                {
                    DataActionMet(false, v);
                    return;
                }

                StringCollection stringCollection = data.GetFileDropList();
                if (stringCollection.Count != 0)
                {
                    DataActionMet(true, stringCollection[0]);
                    return;
                }
            }
        }

        #endregion

    }
}

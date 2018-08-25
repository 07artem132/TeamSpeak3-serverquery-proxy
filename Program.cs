using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using Json;

namespace TeamSpeak3ServerQueryProxy
{
    internal static class Program
    {
        private static Dictionary<int, Thread> _threadPollProxy = new Dictionary<int, Thread>();

        private static string LoadConfig()
        {
            var fstream = File.OpenRead(Environment.CurrentDirectory + "/config.json");
            var array = new byte[fstream.Length];
            fstream.Read(array, 0, array.Length);
            var configString = System.Text.Encoding.Default.GetString(array);
            Console.WriteLine("Config: " + configString);
            return configString;
        }

        public static void Main()
        {
            var config = Json.JsonParser.Deserialize(LoadConfig());

            foreach (var item in config)
            {
                var proxy = new Proxy();

                var packetChanger = new PacketChanger();
                packetChanger.setConfig(item.packet_changer);

                var whiteListControl = new WhiteListControl();
                whiteListControl.SetConfig(item.white_list);

                _threadPollProxy.Add((int) item.server_id,
                    new Thread(() => proxy.Run((int) item.listen.port, (string) item.listen.ip, (int) item.remote.port,
                        (string) item.remote.ip, packetChanger, whiteListControl)));
            }

            foreach (var thread in _threadPollProxy)
            {
                thread.Value.Start();
            }

            Console.Read();
        }
    }


    public class PacketChanger
    {
        //      private Dictionary<string, string> _loginComandChange = new Dictionary<string, string>();
        //    private HashSet<int> _saveVirtualServerId;
        public void setConfig(JsonObject config)
        {
        }

        public PacketChanger()
        {
            //Format:
            //FROMADMIN FROMPASS ADMIN1 PASS1
            /*       foreach (var item in changes)
                   {
                       var tx = item.Split((' '));
                       _loginComandChange.Add(tx[0] + " " + tx[1], tx[2] + " " + tx[3]);
                   }
       
                   this._saveVirtualServerId = new HashSet<int>(saveVirtualServerId);*/
        }

        public string Process(string input)
        {
            /*     //login ADMIN PASS
                 if (input.Contains("login"))
                 {
                     input = input.Replace("client_login_name=", "").Replace("client_login_password=", "");
                     foreach (var item in _loginComandChange)
                         if (input.Contains(item.Key))
                             return "login " + item.Value + Environment.NewLine;
                 }
     
                 if (input.Contains("virtualserver_id"))
                 {
                     var serverListRight = new Dictionary<int, Dictionary<string, string>>();
                     var serverList = input.Split('|');
     
                     for (var index = 0; index < serverList.Length; ++index)
                     {
                         var serverInfo = serverList[index].Split(' ')
                             .Select(value => value.Split('=')).ToDictionary(pair => pair[0], pair => pair[1]);
     
                         var id = int.Parse(serverInfo["virtualserver_id"]);
                         if (_saveVirtualServerId.Contains(id))
                             serverListRight.Add(id, serverInfo);
                     }
     
                     input = "";
                     foreach (var item in serverListRight)
                     {
                         foreach (var x in item.Value)
                         {
                             input += x.Key + "=" + x.Value + " ";
                         }
     
                         input = input.Substring(0, input.Length - 1);
                         input += "|";
                     }
     
                     input = input.Substring(0, input.Length - 1);
                 }
     */
            return input;
        }
    }

    public class WhiteListControl
    {
        private List<string> _ipList = new List<string>();


        public void SetConfig(List<object> config)
        {
            foreach (var ip in config)
            {
                _ipList.Add(ip.ToString());
            }
        }

        public bool Allow(string ip)
        {
            return _ipList.Contains(ip);
        }
    }

    public class Proxy
    {
        private TcpListener _server = null;
        private PacketChanger _packetChanger;

        private volatile bool _isActive = true;

        public void Run(int listenPort, string listenIp, int remotePort, string remoteIp, PacketChanger packetChanger,
            WhiteListControl whiteListControl)
        {
            this._packetChanger = packetChanger;

            var log = new Loger(listenIp);

            try
            {
                _server = new TcpListener(IPAddress.Parse(listenIp), listenPort);

                _server.Start();
                log.Info("listen " + listenIp + ":" + listenPort + " resend to " + remoteIp + ":" + remotePort);

                while (_isActive)
                {
                    log.Info("Waiting for a connection... ");

                    var client = _server.AcceptTcpClient();

                    ThreadPool.QueueUserWorkItem(x =>
                    {
                        var temp = new byte[65535 * 4];
                        var clientIp = ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString();

                        log.Info("connected!", clientIp);

                        if (!whiteListControl.Allow(clientIp))
                        {
                            log.Info("client disconnected, ip not allowed", clientIp);
                            client.Close();
                            return;
                        }


                        var remoteHost = new TcpClient();

                        remoteHost.Connect(remoteIp, remotePort);

                        if (!remoteHost.Connected)
                        {
                            log.Error("remote host " + remoteIp + ":" + remotePort + " could not connect.");
                            client.Close();
                            return;
                        }

                        log.Info("remote host connected", clientIp, remoteIp + ":" + remotePort);

                        var streamRemoteHost = remoteHost.GetStream();
                        var clientStream = client.GetStream();

                        remoteHost.ReceiveTimeout = 50000;
                        client.ReceiveTimeout = 50000;

                        try
                        {
                            while (_isActive)
                            {
                                var bytes = remoteHost.Client.Receive(temp);
                                if (bytes > 0)
                                {
                                    ChangeHelperServer(log, ref temp, ref bytes, clientIp, remoteIp + ":" + remotePort);
                                    client.Client.Send(temp, bytes, SocketFlags.None);
                                }

                                bytes = client.Client.Receive(temp);
                                if (bytes > 0)
                                {
                                    ChangeHelperClient(log, ref temp, ref bytes,clientIp, remoteIp + ":" + remotePort);
                                    remoteHost.Client.Send(temp, bytes, SocketFlags.None);
                                }


                                if ((remoteHost.Client.Poll(1, SelectMode.SelectRead) &&
                                     remoteHost.Client.Available == 0) ||
                                    (client.Client.Poll(1, SelectMode.SelectRead) &&
                                     client.Client.Available == 0))
                                {
                                    client.Close();
                                    remoteHost.Close();
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error("SocketException: " + e);
                            client.Close();
                            remoteHost.Close();
                        }
                    });
                }
            }
            catch (Exception e)
            {
                log.Error("SocketException: " + e);
            }
        }

        private void ChangeHelperClient(Loger loger, ref byte[] temp, ref int bytesCount, string clientIp,
            string remoteHost)
        {
            var text = Encoding.UTF8.GetString(temp, 0, bytesCount);
            text = _packetChanger.Process(text);
            bytesCount = Encoding.UTF8.GetBytes(text, 0, text.Length, temp, 0);
            loger.Info("recive client:  " + text, clientIp, remoteHost);
        }

        private void ChangeHelperServer(Loger loger, ref byte[] temp, ref int bytesCount, string clientIp,
            string remoteHost)
        {
            var text = Encoding.UTF8.GetString(temp, 0, bytesCount);
            text = _packetChanger.Process(text);
            bytesCount = Encoding.UTF8.GetBytes(text, 0, text.Length, temp, 0);
            loger.Info("recive server: " + text, clientIp, remoteHost);
        }
    }


    public class Loger
    {
        private string _ipAddress;
        private bool _writeFile;

        public Loger(string ipAddress, bool writeFile = false)
        {
            _ipAddress = ipAddress;
            _writeFile = writeFile;
        }

        public void Error(string message, string clientIp = "", string remoteHost = "")
        {
            Write(MessageFormat(message, "error", clientIp, remoteHost));
        }

        public void Warning(string message, string clientIp = "", string remoteHost = "")
        {
            Write(MessageFormat(message, "warning", clientIp, remoteHost));
        }

        public void Info(string message, string clientIp = "", string remoteHost = "")
        {
            Write(MessageFormat(message, "info", clientIp, remoteHost));
        }

        public void Debug(string message, string clientIp = "", string remoteHost = "")
        {
            Write(MessageFormat(message, "debug", clientIp, remoteHost));
        }

        private void Write(string message)
        {
            Console.WriteLine(message);
        }

        private string MessageFormat(string message, string logLevel = "", string clientIp = "",
            string remoteHost = "")
        {
            var formatedMessage = "";

            if (logLevel != "")
            {
                formatedMessage += "[" + logLevel + "]";
            }

            if (clientIp != "")
            {
                formatedMessage += " src ip " + clientIp;
            }

            if (remoteHost != "")
            {
                formatedMessage += " proxy to " + remoteHost;
            }

            formatedMessage += " " + message;

            return formatedMessage;
        }
    }
}
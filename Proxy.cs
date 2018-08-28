using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Json;

namespace TeamSpeak3ServerQueryProxy
{
    public class Proxy
    {
        private TcpListener _server = null;
        private readonly PacketChanger _packetChanger;
        private readonly WhiteListControl _whiteListControl;
        private bool _isActive = true;
        private readonly Thread _thread;
        private readonly int _listenPort;
        private readonly string _listenIp;
        private readonly int _remotePort;
        private readonly string _remoteIp;
        private readonly Loger _log;

        public Proxy(
            int listenPort,
            string listenIp,
            int remotePort,
            string remoteIp,
            PacketChangerConfig packetChangerConfigConfig,
            IEnumerable<object> whiteListConfig)
        {
            this._packetChanger = new PacketChanger(packetChangerConfigConfig);
            this._whiteListControl = new WhiteListControl(whiteListConfig);
            this._log = new Loger(_listenIp);

            this._listenPort = listenPort;
            this._listenIp = listenIp;
            this._remotePort = remotePort;
            this._remoteIp = remoteIp;

            this._thread = new Thread(this.Run);

            this._thread.Start();
        }

        public void Stop()
        {
            this._isActive = false;
            Console.Write("thread stop");
            this._server.Server?.Close();
            this._thread.Abort();
        }

        private void Run()
        {
            try
            {
                _server = new TcpListener(IPAddress.Parse(_listenIp), _listenPort);

                _server.Start();
                this._log.Info(
                    "listen " + _listenIp + ":" + _listenPort + " resend to " + _remoteIp + ":" + _remotePort);

                while (this._isActive)
                {
                    this._log.Debug("Waiting for a connection... ");

                    var client = _server.AcceptTcpClient();
                    client.NoDelay = true;
                    client.ReceiveTimeout = 50000;
                    this._log.Debug("tcp client accepted");

                    ThreadPool.QueueUserWorkItem(x =>
                    {
                        var temp = new byte[1500 * 6];
                        var clientIp = ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString();

                        this._log.Debug("connected!", clientIp);

                        if (!this._whiteListControl.Allow(clientIp))
                        {
                            this._log.Info("client disconnected, ip not allowed", clientIp);
                            client.Close();
                            return;
                        }


                        var remoteHost = new TcpClient {Client = {NoDelay = true, ReceiveTimeout = 50000}};

                        try
                        {
                            remoteHost.Connect(_remoteIp, _remotePort);
                        }
                        catch (Exception e)
                        {
                            this._log.Error("remote host " + _remoteIp + ":" + _remotePort + " could not connect.");
                            client.Close();
                            return;
                        }

                        this._log.Debug("remote host connected", clientIp, _remoteIp + ":" + _remotePort);

                        try
                        {
                            while (this._isActive)
                            {
                                while (remoteHost.Client.Available > 0)
                                {
                                    var bytes = remoteHost.Client.Receive(temp, temp.Length, SocketFlags.None);
                                    if (bytes <= 0) continue;
                                    ChangeHelperServer(ref temp, ref bytes, clientIp);
                                    client.Client.Send(temp, bytes, SocketFlags.None);
                                }

                                while (client.Client.Available > 0)
                                {
                                    var bytes = client.Client.Receive(temp, temp.Length, SocketFlags.None);
                                    if (bytes <= 0) continue;
                                    ChangeHelperClient(ref temp, ref bytes, clientIp);
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
                            this._log.Error("SocketException: " + e);
                            client.Close();
                            remoteHost.Close();
                        }
                    });
                }
            }
            catch (Exception e)
            {
                this._log.Error("SocketException: " + e);
            }
        }

        private void ChangeHelperClient(ref byte[] temp, ref int bytesCount, string clientIp)
        {
            var decodeTemp = Encoding.UTF8.GetString(temp, 0, bytesCount);
            this._log.Debug("recive client:  " + decodeTemp, clientIp, this._remoteIp + ":" + this._remotePort);
            decodeTemp = this._packetChanger.PacketChangerClient(decodeTemp);
            this._log.Debug("recive client modify:  " + decodeTemp, clientIp, this._remoteIp + ":" + this._remotePort);
            bytesCount = Encoding.UTF8.GetBytes(decodeTemp, 0, decodeTemp.Length, temp, 0);
        }

        private void ChangeHelperServer(ref byte[] temp, ref int bytesCount, string clientIp)
        {
            var text = Encoding.UTF8.GetString(temp, 0, bytesCount);
            //    text = this._packetChanger.Process(text);
            bytesCount = Encoding.UTF8.GetBytes(text, 0, text.Length, temp, 0);
            this._log.Info("recive server: " + text, clientIp, this._remoteIp + ":" + this._remotePort);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace TeamSpeak3ServerQueryProxy
{
    internal static class Program
    {
        private static readonly Dictionary<int, Proxy> ThreadPollProxy = new Dictionary<int, Proxy>();


        public static void Main()
        {
            var config = Config.LoadConfigFromFile("config.json");

            foreach (var server in config)
            {
                ThreadPollProxy.Add((int) server.ServerId, new Proxy((int) server.Listen.Port,
                    server.Listen.Ip,
                    (int) server.Remote.Port,
                    server.Remote.Ip,
                    server.PacketChangerConfig,
                    server.WhiteListConfig));
            }

            // Thread.Sleep(1000 * 10);
            //       foreach (var thread in ThreadPollProxy)
            //       {
            //       thread.Value.Stop();
            //}

//            Console.Write("завершаем программу");
            Console.Read();
        }
    }
}
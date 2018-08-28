using System;

namespace TeamSpeak3ServerQueryProxy
{
    public class Loger
    {
        private string _ipAddress;
        private bool _writeFile;
        private string _fileName;
        private int _logLevel = 0;

        public Loger(string ipAddress, int logLevel = 0, bool writeFile = false, string fileName = "log.txt")
        {
            this._ipAddress = ipAddress;
            this._writeFile = writeFile;
            this._fileName = fileName;
            this._logLevel = logLevel;
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
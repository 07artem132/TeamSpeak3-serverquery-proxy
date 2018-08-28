using System;
using Json;
using System.Collections.Generic;

namespace TeamSpeak3ServerQueryProxy
{
    public partial class PacketChanger
    {
        private readonly Dictionary<string, AuthReplacement> _loginComand =
            new Dictionary<string, AuthReplacement>();

        private bool _fileTransferPort = false;

        public void SetConfig(PacketChangerConfig config)
        {
            this._fileTransferPort = config.FileTransferPort;
            foreach (var item in config.AuthReplacement)
            {
                _loginComand.Add(item.LoginReplace, item);
            }
        }

        public PacketChanger(PacketChangerConfig config)
        {
            this.SetConfig(config);
        }

        public string PacketChangerClient(string input)
        {
            var words = input.IndexOf(" ", 0, StringComparison.Ordinal);

            switch (words > 0 ? input.Substring(0, words) : input.Replace("\n\r", ""))
            {
                case "login":
                    return this.LoginReplacement(input);
                default:
                    return input;
            }
        }

        /*
          login client_login_name={username} client_login_password={password}
          login {username} {password}
         */
        private string LoginReplacement(string input)
        {
            string loginStr, passStr;

            if (input.Contains("client_login_name") && input.Contains("client_login_password"))
            {
                var inputMod = input.Replace("\n\r", " ");
                var iof1 = inputMod.IndexOf("client_login_name", StringComparison.Ordinal) +
                           "client_login_name=".Length;
                var iof2 = inputMod.IndexOf("client_login_password", StringComparison.Ordinal) +
                           "client_login_password=".Length;
                loginStr = inputMod.Substring(iof1, inputMod.IndexOf(" ", iof1, StringComparison.Ordinal) - iof1);
                passStr = inputMod.Substring(iof2, inputMod.IndexOf(" ", iof2, StringComparison.Ordinal) - iof2);
            }
            else
            {
                var inputMod = input.Replace(Environment.NewLine, "");
                var split = inputMod.Split(' ');
                if (split.Length != 3)
                {
                    loginStr = "";
                    passStr = "";
                }
                else
                {
                    loginStr = split[1];
                    passStr = split[2];
                }
            }

            AuthReplacement rep;
            if (_loginComand.TryGetValue(loginStr, out rep) && rep.PasswordReplace == passStr)
                return "login " + rep.ToLogin + " " + rep.ToPassword + Environment.NewLine;

            return input;
        }
    }
}
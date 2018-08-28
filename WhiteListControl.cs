using System.Collections.Generic;

namespace TeamSpeak3ServerQueryProxy
{
    public class WhiteListControl
    {
        private readonly HashSet<string> _ipList = new HashSet<string>();

        public WhiteListControl(IEnumerable<object> config)
        {
            this.SetConfig(config);
        }

        public void SetConfig(IEnumerable<object> config)
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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    [Serializable]
    public class CrawlSettings
    {
        private byte _depth = 3;

        private bool _lockHost;

        private byte _threadCount = 1;

        private int _timeout = 15000;

        private string _userAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11";


        public CrawlSettings()
        {
            AutoSpeedLimit = false;
            EscapeLinks = new List<string>();
            HrefKeywords = new List<string>();
            LockHost = false;
            RegularFilterExpressions = new List<string>();
            SeedsAddress = new List<string>();
        }

        public bool AutoSpeedLimit { get; set; }

        public byte Depth
        {
            get
            {
                return _depth;
            }

            set
            {
                _depth = value;
            }
        }

        public List<string> EscapeLinks { get; private set; }

        public List<string> HrefKeywords { get; private set; }

        public bool LockHost
        {
            get
            {
                return _lockHost;
            }

            set
            {
                _lockHost = value;
            }
        }

        public List<string> RegularFilterExpressions { get; private set; }

        public List<string> SeedsAddress { get; private set; }

        public byte ThreadCount
        {
            get
            {
                return _threadCount;
            }

            set
            {
                _threadCount = value;
            }
        }

        public int Timeout
        {
            get
            {
                return _timeout;
            }

            set
            {
                _timeout = value;
            }
        }

        public string UserAgent
        {
            get
            {
                return _userAgent;
            }

            set
            {
                _userAgent = value;
            }
        }
    }
}

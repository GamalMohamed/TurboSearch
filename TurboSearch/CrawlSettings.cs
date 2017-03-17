using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboSearch
{
    [Serializable]
    public class CrawlSettings
    {
        private byte _depth = 3;

        private bool _lockHost = true;

        private byte _threadCount = 1;

        private int _timeout = 15000;

        private string _userAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11";


        public CrawlSettings()
        {
            this.AutoSpeedLimit = false;
            this.EscapeLinks = new List<string>();
            this.KeepCookie = false;
            this.HrefKeywords = new List<string>();
            this.LockHost = true;
            this.RegularFilterExpressions = new List<string>();
            this.SeedsAddress = new List<string>();
        }

        public bool AutoSpeedLimit { get; set; }

        public byte Depth
        {
            get
            {
                return this._depth;
            }

            set
            {
                this._depth = value;
            }
        }

        public List<string> EscapeLinks { get; private set; }

        public bool KeepCookie { get; set; }

        public List<string> HrefKeywords { get; private set; }

        public bool LockHost
        {
            get
            {
                return this._lockHost;
            }

            set
            {
                this._lockHost = value;
            }
        }

        public List<string> RegularFilterExpressions { get; private set; }

        public List<string> SeedsAddress { get; private set; }

        public byte ThreadCount
        {
            get
            {
                return this._threadCount;
            }

            set
            {
                this._threadCount = value;
            }
        }

        public int Timeout
        {
            get
            {
                return this._timeout;
            }

            set
            {
                this._timeout = value;
            }
        }

        public string UserAgent
        {
            get
            {
                return this._userAgent;
            }

            set
            {
                this._userAgent = value;
            }
        }
    }
}

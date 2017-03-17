using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RobotsTxt;


namespace TurboSearch
{
    class Program
    {
        private static readonly CrawlSettings Settings = new CrawlSettings();
        private static BloomFilter<string> _filter;
        private static readonly System.IO.StreamWriter File = new System.IO.StreamWriter(@"D:\log.txt");

        private static void Main(string[] args)
        {
            _filter = new BloomFilter<string>(200000);

            Settings.SeedsAddress.Add("https://en.wikipedia.org/wiki/Car"); // https://en.wikipedia.org/wiki/Portal:Contents

            Settings.ThreadCount = 2;
            Settings.Depth = 3;
            Settings.EscapeLinks.Add(".jpg");
            Settings.AutoSpeedLimit = false;
            Settings.LockHost = false;

            var spider = new Spider(Settings);
            spider.AddUrlEvent += MasterAddUrlEvent;
            spider.Crawl();
        }

        private static bool MasterAddUrlEvent(AddUrlEventArgs args)
        {
            if (_filter.Contains(args.Url))
                return false;

            string contentsOfRobotsTxtFile = new WebClient().DownloadString(ConvertToBaseUrl(args.Url)+"/robots.txt");
            Robots robots = Robots.Load(contentsOfRobotsTxtFile);

            if (!robots.IsPathAllowed(Settings.UserAgent, args.Url))
            {
                Console.WriteLine("Blocked URL");
                return false;
            }
              
            _filter.Add(args.Url);

            Console.WriteLine(args.Url);

            lock (File)
            {
                File.WriteLine(args.Url);
            }
            

            return true;
        }


        public static string ConvertToBaseUrl(string url)
        {
            var x=url.Split('/');
            return (x[0] + "//" + x[2]);
        }


    }
}


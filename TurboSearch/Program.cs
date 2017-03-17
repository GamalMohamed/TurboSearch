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

            Settings.SeedsAddress.Add("https://blog.codinghorror.com/"); // https://en.wikipedia.org/wiki/Portal:Contents

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
            if (_filter.Contains(args.Url)|| !CheckRobotTxtFile(args.Url))
                return false;
            
            _filter.Add(args.Url);

            Console.WriteLine(args.Url);

            lock (File)
            {
                File.WriteLine(args.Url);
            }
            

            return true;
        }


        private static bool CheckRobotTxtFile(string url)
        {
            var urlPartitions = url.Split('/');
            string domain = (urlPartitions[0] + "//" + urlPartitions[2]);

            string contentsOfRobotsTxtFile = new WebClient().DownloadString(domain + "/robots.txt");
            Robots robots = Robots.Load(contentsOfRobotsTxtFile);

            if (!robots.IsPathAllowed(Settings.UserAgent, url))
            {
                Console.WriteLine("Blocked URL");
                return false;
            }
            return true;
        }
        
    }
}


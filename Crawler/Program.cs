using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RobotsTxt;


namespace Crawler
{
    class Program
    {
        private static BloomFilter<string> _filter;

        private static readonly CrawlSettings Settings = new CrawlSettings();

        public static Spider MySpider;

        private static void Main(string[] args)
        {
            _filter = new BloomFilter<string>(200000);

            Settings.ThreadCount = 10;
            Settings.Depth = 4;
            Settings.EscapeLinks.Add(".jpg");

            MySpider = new Spider(Settings)
            {
                Logfilepath =
                    @"D:\3rd year-2nd term material\1- APT\3- Project\Project 2017\TurboSearch\Crawler\bin\log.txt"
            };

            SetLogSettings();

            MySpider.AddUrlEvent += MasterAddUrlEvent;
            MySpider.Crawl();

        }

        private static void SetLogSettings()
        {
            if (!File.Exists(MySpider.Logfilepath)
                || new FileInfo(MySpider.Logfilepath).Length == 0
                || new FileInfo(MySpider.Logfilepath).Length == 1)
            {
                Console.WriteLine("Starting a new Crawl round:\n");
                MySpider.LogFile = new StreamWriter(MySpider.Logfilepath);
                Settings.SeedsAddress.Add("https://en.wikipedia.org/wiki/Portal:Contents"); 
            }
            else
            {
                Console.WriteLine("Continuing crawling..reading from Cache:\n");
                var lines = File.ReadAllLines(MySpider.Logfilepath);
                for (int i = 1; i < Settings.ThreadCount && i < lines.Length; i++)
                {
                    Settings.SeedsAddress.Add(lines[lines.Length - i]);
                }
                MySpider.CrawledLinksNumber = Math.Min(lines.Length, Math.Abs(lines.Length - Settings.ThreadCount));
                MySpider.LogFile = new StreamWriter(MySpider.Logfilepath);
                foreach (string l in lines)
                {
                    MySpider.LogFile.WriteLine(l);
                    MySpider.LogFile.Flush();
                }
                MySpider.LogFile.WriteLine();
            }    
        }


        private static bool MasterAddUrlEvent(AddUrlEventArgs args)
        {
            lock (_filter)
            {
                if (MySpider.CrawledLinksNumber >= 5000)
                {
                    Console.WriteLine("\nReached max no. of pages to crawl!Exiting..\n");
                    Environment.Exit(1);
                }

                if (CheckRobotTxtFile(args.Url) && !_filter.Contains(args.Url))
                {
                    _filter.Add(args.Url);

                    Console.WriteLine(MySpider.CrawledLinksNumber++ + ". " + args.Url);
                    WebClient client = new WebClient();
                    client.DownloadFile(args.Url, @"D:\temp\" + MySpider.CrawledLinksNumber + ".html");

                    MySpider.LogFile.WriteLine(args.Url);
                    MySpider.LogFile.Flush();

                    return true;
                }
            }

            return false;
        }
        
        private static bool CheckRobotTxtFile(string url)
        {
            var urlPartitions = url.Split('/');
            string domain = (urlPartitions[0] + "//" + urlPartitions[2]);

            string contentsOfRobotsTxtFile = new WebClient().DownloadString(domain + "/robots.txt");
            Robots robots = Robots.Load(contentsOfRobotsTxtFile);

            if (!robots.IsPathAllowed(Settings.UserAgent, url))
            {
                //Console.WriteLine("Blocked URL");
                return false;
            }
            return true;
        }

    }
}


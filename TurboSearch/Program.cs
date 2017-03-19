using System;
using System.Collections.Generic;
using System.IO;
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

        private static string logfilepath =
            @"D:\3rd year-2nd term material\1- APT\3- Project\Project 2017\TurboSearch\TurboSearch\bin\log.txt";
        private static BloomFilter<string> _filter;
        public static StreamWriter LogFile;
        private static int _counter;

        private static void Main(string[] args)
        {
            _filter = new BloomFilter<string>(200000);

            Settings.ThreadCount = 5;
            Settings.Depth = 3;
            Settings.EscapeLinks.Add(".jpg");
            Settings.AutoSpeedLimit = false;
            Settings.LockHost = false;

            if (!File.Exists(logfilepath) || new FileInfo(logfilepath).Length == 0 || new FileInfo(logfilepath).Length == 1)
            {
                Console.WriteLine("File doesn't exist or empty");
                LogFile = new StreamWriter(logfilepath);
                Settings.SeedsAddress.Add("https://blog.codinghorror.com");  // https://en.wikipedia.org/wiki/Portal:Contents
            }
            else
            {
                Console.WriteLine("File already written in");
                var lines = File.ReadAllLines(logfilepath);
                for (int i = 1; i < Settings.ThreadCount && i < lines.Length; i++)
                {
                    Settings.SeedsAddress.Add(lines[lines.Length - i]);
                }
                _counter = Math.Min(lines.Length,lines.Length - Settings.ThreadCount);
                LogFile = new StreamWriter(logfilepath);
                foreach (string l in lines)
                {
                    LogFile.WriteLine(l);
                    LogFile.Flush();
                }
                LogFile.WriteLine();
            }
            
            var spider = new Spider(Settings);
            spider.AddUrlEvent += MasterAddUrlEvent;
            spider.Crawl();
        }

        private static bool MasterAddUrlEvent(AddUrlEventArgs args)
        {
            lock (_filter)
            {
                if (CheckRobotTxtFile(args.Url) && !_filter.Contains(args.Url))
                {
                    _filter.Add(args.Url);

                    Console.WriteLine(_counter++ + ". " + args.Url);
                    WebClient client = new WebClient();
                    client.DownloadFile(args.Url, @"D:\temp\" + _counter + ".html");

                    LogFile.WriteLine(args.Url);
                    LogFile.Flush();
                }
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
                //Console.WriteLine("Blocked URL");
                return false;
            }
            return true;
        }

    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace TurboSearch
{
    public delegate void DataReceivedEventHandler(DataReceivedEventArgs args);
    public delegate void CrawlErrorEventHandler(CrawlErrorEventArgs args);
    public delegate bool AddUrlEventHandler(AddUrlEventArgs args);

    public class DataReceivedEventArgs : EventArgs
    {
        public int Depth { get; set; }
        public string Html { get; set; }
        public string Url { get; set; }
    }
    public class CrawlErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public string Url { get; set; }
    }
    public class AddUrlEventArgs : EventArgs
    {
        public int Depth { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }

    public class Spider
    {
        private const string WebUrlRegularExpressions = @"^(http|https)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";

        private readonly Random _random;
        private readonly bool[] _threadStatus;
        private readonly Thread[] _threads;

        public event AddUrlEventHandler AddUrlEvent;
        public event CrawlErrorEventHandler CrawlErrorEvent;
        public event DataReceivedEventHandler DataReceivedEvent;

        public string Logfilepath { get; set; }
        public StreamWriter LogFile { get; set; }
        public int CrawledLinksNumber { get; set; }
        
        public CrawlSettings Settings { get; }

        public Spider(CrawlSettings settings)
        {
            _random = new Random();

            Settings = settings;
            _threads = new Thread[settings.ThreadCount];
            _threadStatus = new bool[settings.ThreadCount];
        }

        public void Crawl()
        {
            Initialize();

            for (int i = 0; i < _threads.Length; i++)
            {
                _threads[i].Start(i);
                _threadStatus[i] = false;
            }
        }

        public void Stop()
        {
            foreach (Thread thread in _threads)
            {
                thread.Abort();
            }
        }

        private void ConfigRequest(HttpWebRequest request)
        {
            request.UserAgent = Settings.UserAgent;
            request.AllowAutoRedirect = true;
            request.MediaType = "text/html";
            request.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";

            if (Settings.Timeout > 0)
            {
                request.Timeout = Settings.Timeout;
            }
        }

        private void CrawlProcess(object threadIndex)
        {
            var currentThreadIndex = (int)threadIndex;
            while (true)
            {
                if (UrlQueue.Instance.Count == 0)
                {
                    _threadStatus[currentThreadIndex] = true;
                    if (!_threadStatus.Any(t => t == false))
                    {
                        break;
                    }

                    Thread.Sleep(2000);
                    continue;
                }

                _threadStatus[currentThreadIndex] = false;

                if (UrlQueue.Instance.Count == 0)
                {
                    continue;
                }

                UrlInfo urlInfo = UrlQueue.Instance.DeQueue();

                HttpWebRequest request = null;
                HttpWebResponse response = null;

                try
                {
                    if (urlInfo == null)
                    {
                        continue;
                    }

                    if (Settings.AutoSpeedLimit)
                    {
                        int span = _random.Next(1000, 5000);
                        Thread.Sleep(span);
                    }

                    request = WebRequest.Create(urlInfo.UrlString) as HttpWebRequest;
                    ConfigRequest(request);

                    if (request != null)
                    {
                        response = request.GetResponse() as HttpWebResponse;
                    }

                    if (response != null)
                    {
                        Stream stream = null;

                        if (response.ContentEncoding == "gzip")
                        {
                            Stream responseStream = response.GetResponseStream();
                            if (responseStream != null)
                            {
                                stream = new GZipStream(responseStream, CompressionMode.Decompress);
                            }
                        }
                        else
                        {
                            stream = response.GetResponseStream();
                        }

                        using (stream)
                        {
                                
                            string html = ParseContent(stream, response.CharacterSet);

                            ParseLinks(urlInfo, html);

                            if (DataReceivedEvent != null)
                            {
                                DataReceivedEvent(
                                    new DataReceivedEventArgs
                                    {
                                        Url = urlInfo.UrlString,
                                        Depth = urlInfo.Depth,
                                        Html = html
                                    });
                            }
                            stream?.Close();
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (CrawlErrorEvent != null)
                    {
                        if (urlInfo != null)
                        {
                            CrawlErrorEvent(
                                new CrawlErrorEventArgs { Url = urlInfo.UrlString, Exception = exception });
                        }
                    }
                }
                finally
                {
                    request?.Abort();

                    response?.Close();
                }
            }
        }

        private void Initialize()
        {
            if (Settings.SeedsAddress != null && Settings.SeedsAddress.Count > 0)
            {
                foreach (string seed in Settings.SeedsAddress)
                {
                    if (Regex.IsMatch(seed, WebUrlRegularExpressions, RegexOptions.IgnoreCase))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(seed) { Depth = 1 });
                    }
                }
            }

            for (int i = 0; i < Settings.ThreadCount; i++)
            {
                var threadStart = new ParameterizedThreadStart(CrawlProcess);

                _threads[i] = new Thread(threadStart);
            }

            ServicePointManager.DefaultConnectionLimit = 256;
        }

        private bool IsMatchRegular(string url)
        {
            bool result = false;

            if (Settings.RegularFilterExpressions != null && Settings.RegularFilterExpressions.Count > 0)
            {
                if (
                    Settings.RegularFilterExpressions.Any(
                        pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase)))
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }

            return result;
        }

        private string ParseContent(Stream stream, string characterSet)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            byte[] buffer = memoryStream.ToArray();

            Encoding encode = Encoding.ASCII;
            string html = encode.GetString(buffer);

            string localCharacterSet = characterSet;

            Match match = Regex.Match(html, "<meta([^<]*)charset=([^<]*)\"", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                localCharacterSet = match.Groups[2].Value;

                var stringBuilder = new StringBuilder();
                foreach (char item in localCharacterSet)
                {
                    if (item == ' ')
                    {
                        break;
                    }

                    if (item != '\"')
                    {
                        stringBuilder.Append(item);
                    }
                }

                localCharacterSet = stringBuilder.ToString();
            }

            if (string.IsNullOrEmpty(localCharacterSet))
            {
                localCharacterSet = characterSet;
            }

            if (!string.IsNullOrEmpty(localCharacterSet))
            {
                encode = Encoding.GetEncoding(localCharacterSet);
            }

            memoryStream.Close();

            return encode.GetString(buffer);
        }

        private void ParseLinks(UrlInfo urlInfo, string html)
        {
            if (Settings.Depth > 0 && urlInfo.Depth >= Settings.Depth)
            {
                return;
            }

            var urlDictionary = new Dictionary<string, string>();

            Match match = Regex.Match(html, "(?i)<a .*?href=\"([^\"]+)\"[^>]*>(.*?)</a>");
            while (match.Success)
            {
                string urlKey = match.Groups[1].Value;

                string urlValue = Regex.Replace(match.Groups[2].Value, "(?i)<.*?>", string.Empty);

                urlDictionary[urlKey] = urlValue;
                match = match.NextMatch();
            }

            foreach (var item in urlDictionary)
            {
                string href = item.Key;
                string text = item.Value;

                if (!string.IsNullOrEmpty(href))
                {
                    bool canBeAdd = true;

                    if (Settings.EscapeLinks != null && Settings.EscapeLinks.Count > 0)
                    {
                        if (Settings.EscapeLinks.Any(suffix => href.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                        {
                            canBeAdd = false;
                        }
                    }

                    if (Settings.HrefKeywords != null && Settings.HrefKeywords.Count > 0)
                    {
                        if (!Settings.HrefKeywords.Any(href.Contains))
                        {
                            canBeAdd = false;
                        }
                    }

                    if (canBeAdd)
                    {
                        string url = href.Replace("%3f", "?")
                            .Replace("%3d", "=")
                            .Replace("%2f", "/")
                            .Replace("&amp;", "&");

                        if (string.IsNullOrEmpty(url) || url.StartsWith("#")
                            || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                            || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var baseUri = new Uri(urlInfo.UrlString);
                        Uri currentUri = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                             ? new Uri(url)
                                             : new Uri(baseUri, url);

                        url = currentUri.AbsoluteUri;

                        if (Settings.LockHost)
                        {
                            if (baseUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b)
                                != currentUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b))
                            {
                                continue;
                            }
                        }

                        if (!IsMatchRegular(url))
                        {
                            continue;
                        }

                        var addUrlEventArgs = new AddUrlEventArgs { Title = text, Depth = urlInfo.Depth + 1, Url = url };
                        if (AddUrlEvent != null && !AddUrlEvent(addUrlEventArgs))
                        {
                            continue;
                        }

                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = urlInfo.Depth + 1 });
                    }
                }
            }
        }    
    }
}

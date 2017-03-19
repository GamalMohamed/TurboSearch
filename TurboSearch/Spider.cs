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

        private readonly CookieContainer _cookieContainer;

        private readonly Random _random;

        private readonly bool[] _threadStatus;

        private readonly Thread[] _threads;

        public event AddUrlEventHandler AddUrlEvent;
        public event CrawlErrorEventHandler CrawlErrorEvent;
        public event DataReceivedEventHandler DataReceivedEvent;

        public CrawlSettings Settings { get; private set; }

        public Spider(CrawlSettings settings)
        {
            this._cookieContainer = new CookieContainer();
            this._random = new Random();

            this.Settings = settings;
            this._threads = new Thread[settings.ThreadCount];
            this._threadStatus = new bool[settings.ThreadCount];
        }

        public void Crawl()
        {
            this.Initialize();

            for (int i = 0; i < this._threads.Length; i++)
            {
                this._threads[i].Start(i);
                this._threadStatus[i] = false;
            }
        }

        public void Stop()
        {
            foreach (Thread thread in this._threads)
            {
                thread.Abort();
            }
        }

        private void ConfigRequest(HttpWebRequest request)
        {
            request.UserAgent = this.Settings.UserAgent;
            request.CookieContainer = this._cookieContainer;
            request.AllowAutoRedirect = true;
            request.MediaType = "text/html";
            request.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";

            if (this.Settings.Timeout > 0)
            {
                request.Timeout = this.Settings.Timeout;
            }
        }

        private void CrawlProcess(object threadIndex)
        {
            var currentThreadIndex = (int)threadIndex;
            while (true)
            {
                if (UrlQueue.Instance.Count == 0)
                {
                    this._threadStatus[currentThreadIndex] = true;
                    if (!this._threadStatus.Any(t => t == false))
                    {
                        break;
                    }

                    Thread.Sleep(2000);
                    continue;
                }

                this._threadStatus[currentThreadIndex] = false;

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

                    if (this.Settings.AutoSpeedLimit)
                    {
                        int span = this._random.Next(1000, 5000);
                        Thread.Sleep(span);
                    }

                    request = WebRequest.Create(urlInfo.UrlString) as HttpWebRequest;
                    this.ConfigRequest(request);

                    if (request != null)
                    {
                        response = request.GetResponse() as HttpWebResponse;
                    }

                    if (response != null)
                    {
                        this.PersistenceCookie(response);

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
                                
                            string html = this.ParseContent(stream, response.CharacterSet);

                            this.ParseLinks(urlInfo, html);

                            if (this.DataReceivedEvent != null)
                            {
                                this.DataReceivedEvent(
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
                    if (this.CrawlErrorEvent != null)
                    {
                        if (urlInfo != null)
                        {
                            this.CrawlErrorEvent(
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
            if (this.Settings.SeedsAddress != null && this.Settings.SeedsAddress.Count > 0)
            {
                foreach (string seed in this.Settings.SeedsAddress)
                {
                    if (Regex.IsMatch(seed, WebUrlRegularExpressions, RegexOptions.IgnoreCase))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(seed) { Depth = 1 });
                    }
                }
            }

            for (int i = 0; i < this.Settings.ThreadCount; i++)
            {
                var threadStart = new ParameterizedThreadStart(this.CrawlProcess);

                this._threads[i] = new Thread(threadStart);
            }

            ServicePointManager.DefaultConnectionLimit = 256;
        }

        private bool IsMatchRegular(string url)
        {
            bool result = false;

            if (this.Settings.RegularFilterExpressions != null && this.Settings.RegularFilterExpressions.Count > 0)
            {
                if (
                    this.Settings.RegularFilterExpressions.Any(
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
            if (this.Settings.Depth > 0 && urlInfo.Depth >= this.Settings.Depth)
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

                    if (this.Settings.EscapeLinks != null && this.Settings.EscapeLinks.Count > 0)
                    {
                        if (this.Settings.EscapeLinks.Any(suffix => href.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                        {
                            canBeAdd = false;
                        }
                    }

                    if (this.Settings.HrefKeywords != null && this.Settings.HrefKeywords.Count > 0)
                    {
                        if (!this.Settings.HrefKeywords.Any(href.Contains))
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

                        if (this.Settings.LockHost)
                        {
                            if (baseUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b)
                                != currentUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b))
                            {
                                continue;
                            }
                        }

                        if (!this.IsMatchRegular(url))
                        {
                            continue;
                        }

                        var addUrlEventArgs = new AddUrlEventArgs { Title = text, Depth = urlInfo.Depth + 1, Url = url };
                        if (this.AddUrlEvent != null && !this.AddUrlEvent(addUrlEventArgs))
                        {
                            continue;
                        }

                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = urlInfo.Depth + 1 });
                    }
                }
            }
        }

        private void PersistenceCookie(HttpWebResponse response)
        {
            if (!this.Settings.KeepCookie)
            {
                return;
            }

            string cookies = response.Headers["Set-Cookie"];
            if (!string.IsNullOrEmpty(cookies))
            {
                var cookieUri =
                    new Uri(
                        string.Format(
                            "{0}://{1}:{2}/",
                            response.ResponseUri.Scheme,
                            response.ResponseUri.Host,
                            response.ResponseUri.Port));

                this._cookieContainer.SetCookies(cookieUri, cookies);
            }
        }
    }
}

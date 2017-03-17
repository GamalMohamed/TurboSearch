using System;
using System.Collections.Generic;
using System.Collections;
using System.Net;


namespace TurboSearch
{
    class Robot
    {
        // Always use a valid user-agent that describes your crawler
        private static string _robotAgent = "Mozilla 5.0; RobsRobot 1.2; www.strictly-software.com;"; 
        private static string _proxyServer = ""; // details of proxy
        private static WebProxy _proxy;
        private static string _content = string.Empty;
        private static int _statusCode = -1;
        private static string _lastError = string.Empty;
        private static ArrayList _BlockedUrls = new ArrayList();

        

        // Gets the source of an HTML page
        static string getURLContent(string URL)
        {
            //Console.WriteLine("Access URL: " + URL);
            //Console.WriteLine("Proxy: " + _proxyServer);
            //Console.WriteLine("User Agent: " + _robotAgent);
                        
            // reset global vars
            _content = string.Empty;
            _lastError = ""; // will hold the last error if we require details
            _statusCode = -1;

            WebClient client = new WebClient();

            // if we are going through a proxy server
            if (!String.IsNullOrEmpty(_proxyServer))
            {
                if (_proxy == null)
                {
                    // create proxy object on first URL accessed                    
                    _proxy = new WebProxy(_proxyServer, true);
                    client.Proxy = _proxy;
                }
            }

            // set user-agent header and encoding type
            client.Headers["User-Agent"] = _robotAgent; // always set a valid agent!!
            client.Encoding = System.Text.Encoding.UTF8;

            try
            {
                // get the response from the HTTP request (HTML source code)
                _content = client.DownloadString(URL);
                _statusCode = 200; // 200 = OK
            }
            // handle possible errors
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    _lastError = "Bad Domain Name";
                }
                else if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse HTTPResponse = (HttpWebResponse)ex.Response;

                    _statusCode = (int)HTTPResponse.StatusCode;

                    if (HTTPResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        _lastError = "Page Not Found";
                    }
                    else if (HTTPResponse.StatusCode == HttpStatusCode.Forbidden)
                    {
                        _lastError = "Access Forbidden";
                    }
                    else if (HTTPResponse.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        _lastError = "Server Error";
                    }
                    else if (HTTPResponse.StatusCode == HttpStatusCode.Gone)
                    {
                        _lastError = "Page No Longer Available";
                    }
                    else
                    {
                        _lastError = HTTPResponse.StatusDescription;
                    }
                }
                else
                {
                    _lastError = "Error: " + ex.ToString();
                }
            }
            catch (Exception ex)
            {
                _lastError = "Error: " + ex.ToString();
            }
            finally
            {
                client.Dispose();
            }

            if (!String.IsNullOrEmpty(_lastError))
            {
                //Console.WriteLine(_statusCode.ToString() + ": " + _lastError);
            }

            return _content;
        }

        public static bool URLIsAllowed(string URL)
        {            
            // If we have no URLS stored in our blocked array exit now
            if (_BlockedUrls.Count == 0) return true;

            // Convert our string into an Uri object so we can easily access the
            // relative path excluding the host and domain etc.
            Uri checkURL = new Uri(URL);
            URL = checkURL.AbsolutePath.ToLower();

            //Console.WriteLine("Is user-agent: " + _robotAgent + " allowed access to URL: " + URL);

            // if URL is the /robots.txt then don't allow it as we should use the ParseRobotsTxtFile
            // method to parse that file
            if (URL == "/robots.txt")
            {
                return false;
            }
            else
            {
                // loop our array checking for substring matches
                foreach (string blockedURL in _BlockedUrls)
                {
                    if (URL.Length >= blockedURL.Length)
                    {
                        if (URL.Substring(0, blockedURL.Length) == blockedURL)
                        {
                            //Console.WriteLine("Blocked URL: " + blockedURL);
                            // found a DISALLOW command
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        public static void ParseRobotsTxtFile(string URL)
        {

            Uri CurrentURL = new Uri(URL);
            string RobotsTxtFile = "http://" + CurrentURL.Authority + "/robots.txt";

            string FileContents = getURLContent(RobotsTxtFile);
            if (!String.IsNullOrEmpty(FileContents))
            {
                string[] fileLines = FileContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                bool ApplyToBot = false;
                foreach (string line in fileLines)
                {
                    //Console.WriteLine("Line = " + line);
                    RobotCommand CommandLine = new RobotCommand(line);
                    //Console.WriteLine("Command = " + CommandLine.Command + " - URL = " + CommandLine.Url + " - UserAgent: " + CommandLine.UserAgent);

                    switch (CommandLine.Command)
                    {
                        case "COMMENT":   //then comment - ignore
                            break;
                        case "user-agent":   // User-Agent
                            if ((CommandLine.UserAgent.IndexOf("*") >= 0) || (CommandLine.UserAgent.IndexOf(_robotAgent) >= 0))
                            {
                                // these rules apply to our useragent so on next line if its a DISALLOW we will add the URL
                                // to our array of URls we cannot access
                                ApplyToBot = true;
                                //Console.WriteLine(CommandLine.UserAgent + " - Rules apply");
                            }
                            else
                            {
                                ApplyToBot = false;
                            }
                            break;
                        case "disallow":   // Disallow
                            if (ApplyToBot)
                            {
                                // Only add to blocked if the URL is not blank as 
                                // Disallow: 
                                // means to allow access to all URLs
                                if (CommandLine.Url.Length > 0)
                                {
                                    _BlockedUrls.Add(CommandLine.Url.ToLower());
                                    //Console.WriteLine("DISALLOW " + CommandLine.Url);
                                }
                                else
                                {
                                    //Console.WriteLine("ALLOW ALL URLS - BLANK");
                                }
                            }
                            else
                            {
                                //Console.WriteLine("DISALLOW " + CommandLine.Url + " for another user-agent");
                            }
                            break;
                        case "allow":   // Allow - only used by Google
                            //Console.WriteLine("ALLOW: " + CommandLine.Url);
                            break;
                        case "sitemap": // Sitemap - points to Google sitemap
                            //Console.WriteLine("SITEMAP: " + CommandLine.Url);
                            break;
                        default:
                            // empty/unknown/error
                            //Console.WriteLine("# Unrecognised robots.txt entry [" + line + "]");
                            break;
                    }
                }
            }
        }       
    }

    /*
     * This class will take a line from a Robots.txt
     * and parse it to return a command either
     * COMMENT - commented out line
     * USER-AGENT - the name of the useragent to apply the rule to
     * DISALLOW - command to disallow an agent to a Uri
     * ALLOW - Only used by Google - reverse of DISALLOW
     * and also a URL
     */

    class RobotCommand
    {
        private string _Command;
        private string _Url = string.Empty;
        private string _Useragent = string.Empty;

        /*
         * Will convert a robots.txt line [command]:[rule] into either
         * command = user-agent AND useragent = Googlebot
         * OR
         * command = DISALLOW AND URL = /search
         */

        public RobotCommand(string commandline)
        {
            int PosOfComment = commandline.IndexOf('#');
            if (PosOfComment == 0)
            {
                // whole line is a comment
                _Command = "COMMENT";
            }
            else
            {
                // there is a comment on the line so remove it
                if (PosOfComment >= 0)
                {
                    commandline = commandline.Substring(0, PosOfComment);
                }
                // now if we have an instruction
                if (commandline.Length > 0)
                {
                    /* 
                     * split our line on : e.g turn User-agent: GoogleBot
                     * into _Command = User-agent and _Url = GoogleBot                         
                     */
                    string[] lineArray = commandline.Split(':');
                    _Command = lineArray[0].Trim().ToLower();
                    if (lineArray.Length > 1)
                    {
                        // set appropriate property depending on command type
                        if (_Command == "user-agent")
                        {
                            _Useragent = lineArray[1].Trim();
                        }
                        else
                        {
                            _Url = lineArray[1].Trim();
                            // if the URL is a full URL e.g sitemaps then it will contain
                            // a : so add to URL
                            if (lineArray.Length > 2)
                            {
                                _Url += ":"+lineArray[2].Trim();
                            }
                        }

                    }
                }
            }
        }

        public string Command
        {
            get { return _Command; }
        }

        public string Url
        {
            get { return _Url; }
        }

        public string UserAgent
        {
            get { return _Useragent; }
        }
    }

}


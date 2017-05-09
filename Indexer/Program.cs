using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace TurboSearch
{
    class Program
    {
        private static readonly Porter2 Stemer = new Porter2();
        private const string Path = @"D:\Misc\temp0\";
        private const int NumOfDocs = 4500;
        private const long MaxWordsNo = 10000000;

        public static WordsContext Db = new WordsContext();

        private static List<string> ReadStoppingList()
        {
            var stoppingList = new List<string>();

            using (StreamReader sr = new StreamReader(Path + "stopping-list.txt"))
            {

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    stoppingList.Add(line);
                }
                return stoppingList;
            }
        }

        private static bool CheckStoppingList(string str, List<string> stoppingList)
        {
            foreach (string t in stoppingList)
            {
                if (str == t)
                    return false;
            }
            return true;
        }

        private static string appendID_tag(int id, string tag, string str)
        {
            str = str + id.ToString() + '$' + tag;
            return str;
        }

        private static string AppendIDtagWithOccurencies(int occurencies, string str)
        {
            str = str + '$' + occurencies + ',';
            return str;
        }

        private static bool CheckIDexists(int idurl, string str)
        {
            if (str.Length != 0)
            {
                string temp = str;
                temp = temp.Remove(temp.Length - 1);
                int index = temp.LastIndexOf(',');
                string newTemp = temp.Substring(index + 1);
                string[] words = newTemp.Split('$');
                if (words[0] == idurl.ToString())
                    return true;
            }
            return false;
        }

        private static void FillWords(int i, HtmlDocument doc,
            ref int countArr, ref string[] totalWordsofDoc, 
            ref Dictionary<string, string> wordMap, ref Dictionary<string, int> wordOccurs,
            ref string[] wordMapArr)
        {
            char[] delimiters = { };
            string[] tags = { "title", "h1", "h2", "h3", "p" };
            var stoppingList = ReadStoppingList();
            foreach (string t in tags)
            {
                var totalWordsofTag = "";
                switch (t)
                {
                    case "title":
                        totalWordsofTag = (from x in doc.DocumentNode.Descendants()
                                           where x.Name.ToLower() == "title"
                                           select x.InnerText).FirstOrDefault();
                        break;
                    case "p":
                        var sb = new StringBuilder();
                        var nodes = doc.DocumentNode.Descendants().Where(n =>
                            n.NodeType == HtmlNodeType.Text &&
                            n.ParentNode.Name != "script" &&
                            n.ParentNode.Name != "style");
                        totalWordsofTag = "";
                        foreach (HtmlNode node in nodes)
                            totalWordsofTag += node.InnerText;
                        break;
                    default:
                        var hElements = doc.DocumentNode.Descendants(t).Select(nd => nd.InnerText);
                        totalWordsofTag = "";
                        foreach (string node in hElements)
                            totalWordsofTag += node;
                        break;
                }

                var parts = totalWordsofTag.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                string stemmedTotalWordsofTag = "";
                foreach (string word in parts)
                {
                    string sword = Stemer.stem(word.ToLower());
                    if (Regex.IsMatch(sword, @"^[a-z]+$") && CheckStoppingList(sword, stoppingList)
                        && sword != " " && sword != "" && sword != "\n" && sword != "\t")
                    {
                        stemmedTotalWordsofTag = stemmedTotalWordsofTag + sword + ' ';
                    }
                }
                var stemmedTotalWordsofTagArr = stemmedTotalWordsofTag.Split(' ');
                var z = new string[totalWordsofDoc.Length + stemmedTotalWordsofTagArr.Length];
                totalWordsofDoc.CopyTo(z, 0);
                stemmedTotalWordsofTagArr.CopyTo(z, totalWordsofDoc.Length);
                totalWordsofDoc = z;
                foreach (string word in stemmedTotalWordsofTagArr)
                {
                    if (!wordMap.ContainsKey(word))
                    {
                        wordMap[word] = appendID_tag(i, t, "");
                        wordOccurs[word] = 1;
                        wordMapArr[countArr++] = word;
                    }
                    else if (!CheckIDexists(i, wordMap[word]))
                    {
                        wordMap[word] = appendID_tag(i, t, wordMap[word]);
                        wordOccurs[word] = 1;
                    }
                    else
                        wordOccurs[word]++;
                }
            }
        }

        private static void Main(string[] args)
        {
            int countArr = 0;
            var wordMap = new Dictionary<string, string>();
            var wordMapArr = new string[MaxWordsNo];
            var words = new Word[MaxWordsNo];
            for (int i = 0; i < MaxWordsNo; i++)
                words[i] = new Word();

            for (int i = 1; i <= NumOfDocs; i++)
            {
                #region Reading input HTML doc
                var newPath = Path + i + ".html";
                Console.WriteLine("Parsing File: " + newPath);
                if (!File.Exists(newPath))
                    continue;
                var doc = new HtmlDocument();
                doc.Load(newPath);
                #endregion

                #region Processing docs to get words
                var wordOccurs = new Dictionary<string, int>();
                string[] totalWordsofDoc = { "" };

                FillWords(i, doc, ref countArr, ref totalWordsofDoc, 
                    ref wordMap, ref wordOccurs, ref wordMapArr);

                var parts2 = totalWordsofDoc.Distinct().ToArray();
                foreach (string t in parts2)
                    wordMap[t] = AppendIDtagWithOccurencies(wordOccurs[t], wordMap[t]);

                #endregion
            }

            #region Add Words To DB
            Console.WriteLine("Adding {0} words to database", countArr);
            for (int k = 0; k < countArr; k++)
            {
                words[k].SetAttributes(k, wordMapArr[k], wordMap[wordMapArr[k]]);
                Db.Words.Add(words[k]);
                Db.SaveChanges();
                Console.WriteLine(k);
            }
            #endregion
            
        }
    }
}
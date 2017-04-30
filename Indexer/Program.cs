using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Indexer
{
    class Program
    {
        private static readonly char[] Delimiters = { };
        private static readonly Porter2 Stemer = new Porter2();
        private const string Path = @"D:\Misc\temp0\";

        public static WordContext Db = new WordContext();

        private static List<string> ReadStoppingList()
        {
            List<string> stoppingList = new List<string>();

            using (StreamReader sr = new StreamReader(Path+"stopping-list.txt"))
            {

                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    stoppingList.Add(line);
                }
                return stoppingList;
            }
        }

        private static string appendID_tag(int id, string tag, string str)
        {
            str = str + id.ToString() + '$' + '<' + tag + '>';
            return str;
        }

        private static string AppendIDtagWithOccurencies(int occurencies, string str)
        {

            str = str + '$' + occurencies + ',';
            return str;
        }

        private static bool CheckSl(string str, List<string> stoppingList)
        {
            for (int i = 0; i < stoppingList.Count; i++)
            {
                if (str == stoppingList[i])
                    return false;
            }
            return true;
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

        private static void Main(string[] args)
        {

            int countArr = 0;
            Dictionary<string, string> wordMap = new Dictionary<string, string>();
            const long number = 1000000;
            string[] wordMapArr = new string[number];
            var words = new Word[number];
            for (int i = 0; i < number; i++)
            {
                words[i] = new Word();
            }
            string[] tags = { "title", "h1", "h2", "h3", "p" };
            int numOfDocs = 50;
            
            List<string> stoppingList = ReadStoppingList();
            for (int i = 1; i < numOfDocs; i++)
            {
                var newPath = Path + i+".html";
                Console.WriteLine("Parsing File: " + newPath);
                if(!File.Exists(newPath))
                    continue;

                var wordOccurs = new Dictionary<string, int>();
                string[] totalWordsofDoc = { "" };
                var doc = new HtmlDocument();
                doc.Load(newPath);
                foreach (string t in tags)
                {
                    var totalWordsofTag = "";
                    if (t == "title")
                    {
                        totalWordsofTag = (from x in doc.DocumentNode.Descendants()
                            where x.Name.ToLower() == "title"
                            select x.InnerText).FirstOrDefault();

                    }
                    else if (t == "p")
                    {
                        StringBuilder sb = new StringBuilder();
                        IEnumerable<HtmlNode> nodes = doc.DocumentNode.Descendants().Where(n =>
                            n.NodeType == HtmlNodeType.Text &&
                            n.ParentNode.Name != "script" &&
                            n.ParentNode.Name != "style");
                        totalWordsofTag = "";
                        foreach (HtmlNode node in nodes)
                            totalWordsofTag += node.InnerText;
                    }
                    else
                    {
                        var h1Elements = doc.DocumentNode.Descendants(t).Select(nd => nd.InnerText);
                        totalWordsofTag = "";
                        foreach (string node in h1Elements)
                            totalWordsofTag += node;
                    }

                    var parts = totalWordsofTag.Split(Delimiters,
                        StringSplitOptions.RemoveEmptyEntries);
                    string stemmedTotalWordsofTag = "";
                    foreach (string word in parts)
                    {

                        string sword = Stemer.stem(word.ToLower());
                        if (Regex.IsMatch(sword, @"^[a-z]+$") && CheckSl(sword, stoppingList) && sword != " " && sword != "" && sword != "\n" && sword != "\t")
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
                var parts2 = totalWordsofDoc.Distinct().ToArray();
                foreach (string t in parts2)
                    wordMap[t] = AppendIDtagWithOccurencies(wordOccurs[t], wordMap[t]);
            }
            Console.WriteLine("Writing " + countArr + " words to objects");

            for (int k = 0; k < countArr; k++)
            {
                words[k].SetAttributes(k, wordMapArr[k], wordMap[wordMapArr[k]]);
                Db.Words.Add(words[k]);
                Db.SaveChanges();
                Console.WriteLine(k);
            }
            Console.ReadKey();
            for (int k = countArr - 10; k < countArr - 3; k++)
            {
                
                words[k].PrintAttributes();
            }

            Query();

        }

        private static void Query()
        {
            var idWords = (from o in Db.Words
                             select o).Count();
            Console.Write("\nEnter word: ");
            var strin = Console.ReadLine();
            var parts = strin?.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            var parts2 = parts?.Distinct().ToArray();
            for (int i = 0; i < parts2.Length; i++)
                parts2[i] = Stemer.stem(parts2[i].ToLower());

            var allLists = new List<string>[parts2.Length];
            for (int i = 0; i < parts2.Length; i++)
                allLists[i] = new List<string>();

            for (int i = 0; i < parts2.Length; i++)
            {
                for (int k = 0; k < idWords; k++)
                {
                    var e = parts2[i];
                    var d = Db.Words.FirstOrDefault(t => t.WordContent == e);
                    if (d!=null)
                    {
                        string[] newwords = d.WordStorings.Split(',');
                        foreach (string word in newwords)
                        {
                            string[] words2 = word.Split('$');
                            allLists[i].Add(words2[0]);
                        }
                        break;
                    }
                }
            }
            var result = allLists[0];
            for (int i = 1; i < allLists.Length; i++)
            {
                result = result.Intersect(allLists[i]).ToList();
            }
            Console.Write("\nQuery: " + strin + "\nMutual docs: ");
            if (result.Count > 0)
            {
                foreach (var t in result)
                    Console.Write(t + " ");
            }
            else
                Console.Write("No docs!");
            Console.WriteLine("\n");
        }

       
    }
}
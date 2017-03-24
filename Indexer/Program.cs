﻿using HtmlAgilityPack;
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
        private static readonly Porter2 Stemer = new Porter2();
        private static readonly char[] Delimiters = { };

        private const int MaxnumberOfwords = 10000000;
        private const string HtmlDocsPath =
            @"D:\3rd year-2nd term material\1- APT\3- Project\Project 2017\TurboSearch\Indexer\docs\";

        private static readonly List<string> StoppingList = ReadStoppingList();
        public static Word[] WordsDictionary = new Word[MaxnumberOfwords];

        private static void Main(string[] args)
        {
            int idWords = 0;
            string[] tags = { "title", "h1", "h2", "h3", "p" };
            for (int i = 1; i < 30; i++)
            {
                var newPath = HtmlDocsPath + i + ".html";
                Console.WriteLine("Parsing File: " + newPath);

                var doc = new HtmlDocument();
                doc.Load(newPath);

                foreach (var t in tags)
                    FillWordsDictionary(i, doc, t, ref idWords);
            }

            for (int i = 0; i < idWords; i++)
                Console.WriteLine(WordsDictionary[i].Id + " " + WordsDictionary[i].WordContent + " " + WordsDictionary[i].UrlIdTags);

            Query(idWords);
        }

        private static void Query(int idWords)
        {
            Console.Write("\nEnter word: ");
            string strin = Console.ReadLine();
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
                    if (WordsDictionary[k].WordContent == parts2[i])
                    {
                        string[] newwords = WordsDictionary[k].UrlIdTags.Split(',');
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
            Console.WriteLine();
        }

        private static void FillWordsDictionary(int i, HtmlDocument doc, string htmltag, ref int idWords)
        {
            string tagElement = "";
            switch (htmltag)
            {
                case "title":
                    tagElement = doc.DocumentNode.Descendants(htmltag).Select(nd => nd.InnerText).FirstOrDefault();
                    break;
                case "h1":
                case "h2":
                case "h3":
                    var otherTagElements = doc.DocumentNode.Descendants(htmltag).Select(nd => nd.InnerText);
                    foreach (string node in otherTagElements)
                        tagElement += node;
                    break;
                case "p":
                    var sb = new StringBuilder();
                    var nodes = doc.DocumentNode.Descendants().Where(n =>
                        n.NodeType == HtmlNodeType.Text &&
                        n.ParentNode.Name != "script" &&
                        n.ParentNode.Name != "style");
                    foreach (var node in nodes)
                        tagElement += node.InnerText;
                    break;
            }

            var parts = tagElement.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            var parts2 = parts.Distinct().ToArray();
            foreach (var word in parts2)
            {
                string sword = Stemer.stem(word.ToLower());
                if (Regex.IsMatch(sword, @"^[a-z]+$") && CheckStoppingList(sword, StoppingList) && sword != " " && sword != "" && sword != "\n" && sword != "\t")
                {
                    bool flag = false;
                    for (int j = 0; j < idWords; j++)
                    {
                        if (WordsDictionary[j].WordContent == sword)
                        {
                            if (htmltag == "title")
                            {
                                if (!WordsDictionary[j].CheckIDexists(i))
                                {
                                    WordsDictionary[j].appendID_tag(i, "<" + htmltag + ">");
                                    flag = true;
                                }
                            }
                            else
                            {
                                if (WordsDictionary[j].CheckIDexists(i))
                                {
                                    flag = true;
                                    break;
                                }
                                else
                                {
                                    WordsDictionary[j].appendID_tag(i, "<" + htmltag + ">");
                                }
                            }
                        }

                    }
                    if (!flag)
                    {
                        WordsDictionary[idWords] = new Word(idWords, sword, i, "<" + htmltag + ">");
                        idWords++;
                    }
                }
            }
            idWords--;
        }

        private static List<string> ReadStoppingList()
        {
            var stoppingList = new List<string>();
            using (var sr = new StreamReader(HtmlDocsPath + "stopping-list.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    stoppingList.Add(line);
                }
                return stoppingList;
            }
        }

        private static bool CheckStoppingList(string str, IEnumerable<string> stoppingList)
        {
            return stoppingList.All(t => str != t);
        }
    }
}



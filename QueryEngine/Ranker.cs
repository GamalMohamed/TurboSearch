using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace TurboSearch
{
    public class Ranker
    {
        private readonly Search _fetcher;
        private List<Dictionary<string, List<string>>> Tag_DocsIDsList;
        private Dictionary<string, int> _hrefs;
        private Dictionary<string, string> href_docID;

        public Ranker(Search fetcher)
        {
            _fetcher = fetcher;
            Tag_DocsIDsList = new List<Dictionary<string, List<string>>>();
            _hrefs = new Dictionary<string, int>();
            href_docID = new Dictionary<string, string>();
        }

        // Sorts the docs IDs according to relevance and popularity
        public void Rank()
        {
            /*if (_fetcher.SearchType == 1)
                RankWord();
            else if (_fetcher.SearchType == 3)
                RankSentence();*/
            PopularityRanking();

        }

        private void SplitWordInfo(string wordStoring, int i)
        {
            var word_doc = wordStoring.Split(',');
            for (var j = 0; j < word_doc.Length - 1; j++)
            {
                var docId_tag_occ = word_doc[j].Split('$');
                if (Tag_DocsIDsList[i].ContainsKey(docId_tag_occ[1]))
                {
                    Tag_DocsIDsList[i][docId_tag_occ[1]].Add(docId_tag_occ[0]);
                }
                else
                {
                    Tag_DocsIDsList[i].Add(docId_tag_occ[1], new List<string>() { docId_tag_occ[0] });
                }
            }
        }

        private void RankWord()
        {
            Tag_DocsIDsList.Add(new Dictionary<string, List<string>>());
            SplitWordInfo(_fetcher.QueryWordInfo.WordStorings, 0);
            PrintTagSortedDocs();
        }

        private void RankSentence()
        {
            for (var i = 0; i < _fetcher.DistinctqueryWords.Length; i++)
            {
                var word = _fetcher.DistinctqueryWords[i];
                Tag_DocsIDsList.Add(new Dictionary<string, List<string>>());
                if (_fetcher.WordsDictionary.ContainsKey(word))
                {
                    SplitWordInfo(_fetcher.WordsDictionary[word], i);
                }
            }
            PrintTagSortedDocs();
        }

        /*
          Sort Docs according to no. of occs
         */
        private void PopularityRanking()
        {
            ManipulateDocs(_fetcher.Path);
            SortDocsByPopularity();

        }

        private void ManipulateDocs(string path)
        {
            // 1. Read html docs in directory one by one
            var dir = new System.IO.DirectoryInfo(path);
            var filesCount = dir.GetFiles().Length;

            MapLinkId(path); // 2. Map each link to its docID

            for (int i = 1; i <= filesCount; i++)
            {
                var docPath = path + i + ".html";
                if (!File.Exists(docPath))
                    continue;
                var doc = new HtmlDocument();
                doc.Load(docPath);

                Console.WriteLine("extracting hrefs from doc " + i);
                ExtractDocHrefs(doc);
            }
        }

        private void MapLinkId(string path)
        {
            var links = File.ReadAllLines(path + "log.txt");
            for (int i = 1; i <= links.Length; i++)
            {
                href_docID.Add(links[i-1], i.ToString());
            }
        }

        private void ExtractDocHrefs(HtmlDocument doc)
        {
            // Extract hrefs from doc & Add them to a dictionary that holds number of occurences 
            // of each link
            var linkk = doc.DocumentNode.SelectNodes("//a[@href]");
            if (linkk != null)
            {
                foreach (HtmlNode link in linkk)
                {
                    HtmlAttribute att = link.Attributes["href"];
                    if (att.Value[0] == '#')
                        continue;
                    if (_hrefs.ContainsKey(att.Value))
                    {
                        _hrefs[att.Value] = _hrefs[att.Value] + 1;
                    }
                    else
                    {
                        _hrefs.Add(att.Value, 1);
                    }
                }
            }
        }

        private void SortDocsByPopularity()
        {
            
        }

        public void PrintTagSortedDocs()
        {
            string[] tags = { "title", "h1", "h2", "h3", "p" };
            var traversedDoc = new HashSet<string>();
            foreach (var tag in tags)
            {
                Console.WriteLine("{0} tag:\n", tag);
                foreach (var wordDictionary in Tag_DocsIDsList)
                {
                    if (wordDictionary.ContainsKey(tag))
                    {
                        var filterdWordsList = wordDictionary[tag].Intersect(_fetcher.WordsResults).ToList();
                        if (filterdWordsList.Count > 0)
                        {
                            foreach (var d in filterdWordsList)
                            {
                                if (!traversedDoc.Contains(d))
                                {
                                    Console.Write(d + " | ");
                                    traversedDoc.Add(d);
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("\n*********************************");
            }
        }


    }
}

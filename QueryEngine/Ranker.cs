using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboSearch
{
    public class Ranker
    {
        private readonly Search _fetcher;
        private List<Dictionary<string, List<string>>> Tag_DocsIDsList;

        public Ranker(Search fetcher)
        {
            _fetcher = fetcher;
            Tag_DocsIDsList = new List<Dictionary<string, List<string>>>();
        }

        // Sorts the docs IDs according to relevance and popularity
        public void Rank()
        {
            if (_fetcher.SearchType == 1)
                RankWord();
            else if (_fetcher.SearchType == 3)
                RankSentence();

        }

        private void SplitWordInfo(string wordStoring, int i)
        {
            var word_doc = wordStoring.Split(',');
            for (var j = 0; j < word_doc.Length-1; j++)
            {
                var docId_tag_occ = word_doc[j].Split('$');
                if (Tag_DocsIDsList[i].ContainsKey(docId_tag_occ[1]))
                {
                    Tag_DocsIDsList[i][docId_tag_occ[1]].Add(docId_tag_occ[0]);
                }
                else
                {
                    Tag_DocsIDsList[i].Add(docId_tag_occ[1],new List<string>() { docId_tag_occ[0] });
                }
            }
        }

        private void RankWord()
        {
            Tag_DocsIDsList.Add(new Dictionary<string, List<string>>());
            SplitWordInfo(_fetcher.QueryWordInfo.WordStorings,0);
            PrintWordTagSortedDocs(); 
        }

        private void RankSentence()
        {
            for (var i = 0; i < _fetcher.DistinctqueryWords.Length; i++)
            {
                var word = _fetcher.DistinctqueryWords[i];
                Tag_DocsIDsList.Add(new Dictionary<string, List<string>>());
                if (_fetcher.WordsDictionary.ContainsKey(word))
                {
                    SplitWordInfo(_fetcher.WordsDictionary[word],i);
                }
            }
            PrintWordTagSortedDocs();
        }



        public void PrintWordTagSortedDocs()
        {
            string[] tags = {"title", "h1", "h2", "h3", "p"};
            var traversedDoc=new HashSet<string>();
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

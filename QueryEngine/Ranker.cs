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
        private Dictionary<string, string> Tag_DocID;

        public Ranker(Search fetcher)
        {
            _fetcher = fetcher;
            Tag_DocID = new Dictionary<string, string>();
        }

        // Sorts the docs IDs according to relevance and popularity
        public void Rank()
        {
            if (_fetcher.SearchType == 1)
                RankWord();
            else if (_fetcher.SearchType == 2)
                RankSentence();




        }

        private void SplitWordInfo()
        {
            var word_doc = _fetcher.QueryWordInfo.WordStorings.Split(',');
            for (var i = 0; i < word_doc.Length-1; i++)
            {
                var word = word_doc[i];
                var docId_tag_occ = word.Split('$');
                if (Tag_DocID.ContainsKey(docId_tag_occ[1]))
                {
                    var tagDocs = Tag_DocID[docId_tag_occ[1]];
                    Tag_DocID[docId_tag_occ[1]] = tagDocs + " | " + docId_tag_occ[0];
                }
                else
                {
                    Tag_DocID.Add(docId_tag_occ[1], docId_tag_occ[0]);
                }
            }
        }

        private void RankWord()
        {
            SplitWordInfo();
            PrintTagSortedDocs();

            
        }

        private void RankSentence()
        {

        }

        public void PrintTagSortedDocs()
        {
            string[] tags = {"title", "h1", "h2", "h3", "p"};
            foreach (var t in tags)
            {
                if (Tag_DocID.ContainsKey(t))
                {
                    Console.WriteLine("{0} tag:\n {1}",t, Tag_DocID[t]);
                    Console.WriteLine("*********************************");
                }
            }
        }


    }
}

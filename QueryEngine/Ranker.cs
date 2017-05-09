using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboSearch
{
    public class Ranker
    {
        private Search _fetcher;

        public Ranker(Search fetcher)
        {
            _fetcher = fetcher;
        }

        // Sorts the docs IDs according to relevance and popularity
        public void Rank()
        {
            /*
             * 1. Given: docs IDs in fetcher.WordResults, fetcher._inputQuery, fetcher._distictQueryWords
             * 2. Look in dictionary for inputQuery/distictQueryWords & doc ID to get tag & occ #
             * 3. Then, go on!
             */
        }

    }
}

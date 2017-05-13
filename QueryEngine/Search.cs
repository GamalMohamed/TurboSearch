using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace TurboSearch
{
    public class Search
    {
        private readonly Porter2 _stemer;
        private readonly WordsContext _db = new WordsContext();
        private static string _path = @"D:\Misc\temp0\";

        public int SearchType { get; set; }
        public string[] DistinctqueryWords { get; set; }
        public string InputQuery { get; set; }
        public List<string> WordsResults { get; set; } // IDs for docs containing word(s)
        public List<string> PhraseResults { get; set; } // IDs for docs containing the phrase
        public Dictionary<string, string> WordsDictionary { get; set; }
        public Word QueryWordInfo { get; set; }
        public string Path { get; set; }
        public Ranker Ranker { get; set; }

        public Search()
        {
            // Get all words from DB and store it in a dictionary
            WordsDictionary = _db.Words.ToDictionary(t => t.WordContent, t => t.WordStorings);
            _stemer = new Porter2();
            PhraseResults = new List<string>();
            Path = _path;
        }

        public void Query(string input)
        {
            SearchType = ManipulateInputQuery(input);
            if (SearchType == -1)
            {
                Console.WriteLine("Empty string!!");
            }
            else if (SearchType == 1)
            {
                QueryWords(true);
            }
            else if (SearchType == 2)
            {
                PhraseSearch();
            }
            else
            {
                QueryWords(false);
            }
            Console.WriteLine("\n");

            Ranker = new Ranker(this);
            Ranker.Rank();
            Ranker.PrintSortedDocs();
            var x = Ranker.UrlTitleFinalResults;
        }

        public void PrintResults()
        {
            if (InputQuery != null)
            {
                Console.Write("\nQuery: " + InputQuery + "\nMutual docs: ");
                InnerPrint(SearchType == 2 ? PhraseResults : WordsResults);
                Console.WriteLine();
            }
        }

        private void InnerPrint(List<string> results)
        {
            if (results.Count > 0)
            {
                foreach (var t in results)
                    Console.Write(t + " ");
            }
            else
            {
                Console.Write("No docs!");
            }
        }

        // Check for nullity and whether it's query or phrase searching
        private int ManipulateInputQuery(string input)
        {
            if (input.Length == 0) //Empty string
            {
                return -1;
            }
            if (input[0] == '"' && input[input.Length - 1] == '"') // Quoted Phrase
            {
                InputQuery = input.Substring(1, input.Length - 2);
                return 2;
            }

            if (input.Split(' ').Length == 1) //one word
            {
                InputQuery = input;
                return 1;
            }
            else
            {
                InputQuery = input; // sentence
                return 3;
            }
        }

        private void PhraseSearch()
        {
            QueryWords(false); //I now have results list ready!
            Console.WriteLine("No of candidate docs:{0}", WordsResults.Count);
            foreach (var t in WordsResults)
            {
                if (t != "") //just cautious check!
                {
                    var newPath = Path + t + ".html";
                    if (File.ReadAllText(newPath).Contains(InputQuery))
                    {
                        PhraseResults.Add(t);
                    }
                }
            }
        }

        //Split the string and stem each word
        private List<string>[] PreprocessQuery()
        {
            char[] delimiters = { };
            var queryWords = InputQuery.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            DistinctqueryWords = queryWords.Distinct().ToArray();
            for (int i = 0; i < DistinctqueryWords.Length; i++)
                DistinctqueryWords[i] = _stemer.stem(DistinctqueryWords[i].ToLower());

            var allLists = new List<string>[DistinctqueryWords.Length];
            for (int i = 0; i < DistinctqueryWords.Length; i++)
                allLists[i] = new List<string>();

            return allLists;
        }

        // Search for sentence and get each word list
        private List<string>[] SearchForSentence(List<string>[] wordsList)
        {
            for (int i = 0; i < DistinctqueryWords.Length; i++)
            {
                if (WordsDictionary.ContainsKey(DistinctqueryWords[i]))
                {
                    var wordStoring = WordsDictionary[DistinctqueryWords[i]];
                    if (wordStoring != null)
                    {
                        var word_doc = wordStoring.Split(',');
                        foreach (var word in word_doc)
                        {
                            var docId_tag_occ = word.Split('$');
                            wordsList[i].Add(docId_tag_occ[0]);
                        }
                    }
                }
            }
            return wordsList;
        }

        private List<string> SearchForWord()
        {
            var stemmedWord = _stemer.stem(InputQuery.ToLower()); //Preprocess word

            var wordList = new List<string>();
            if (WordsDictionary.ContainsKey(stemmedWord))
            {
                var wordStoring = WordsDictionary[stemmedWord];
                var word_doc = wordStoring.Split(',');
                foreach (var word in word_doc)
                {
                    var docId_tag_occ = word.Split('$');
                    wordList.Add(docId_tag_occ[0]);
                }
                QueryWordInfo = new Word()
                {
                    Id = 1,WordContent = stemmedWord, WordStorings = wordStoring
                };
            }
            return wordList;
        }

        private void QueryWords(bool isWord)
        {
            if (!isWord)
            {
                var wordsList = SearchForSentence(PreprocessQuery());

                // Combining results for sentences
                foreach (var t in wordsList)
                {
                    if (t.Count > 0)
                    {
                        WordsResults = t;
                        break;
                    }
                }
                for (int i = 1; i < wordsList.Length; i++)
                {
                    if (wordsList[i].Count > 0)
                    {
                        WordsResults = WordsResults.Intersect(wordsList[i]).ToList();
                    }
                }
            }
            else
            {
                WordsResults = SearchForWord();
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace TurboSearch
{
    public class Search
    {
        private readonly Porter2 _stemer;
        private const string Path = @"D:\Misc\temp0\";
        private string _inputQuery;
        private int _searchType;
        private string[] _distinctqueryWords;
        private List<string> _wordsResults;
        private readonly List<string> _phraseResults = new List<string>();

        public Dictionary<string, string> WordsDictionary { get; set; }

        public Search(WordsContext db)
        {
            // Get all words from DB and store it in a dictionary
            WordsDictionary = db.Words.ToDictionary(t => t.WordContent, t => t.WordStorings);
            _stemer = new Porter2();
        }

        public void Query(string input)
        {
            _searchType = ManipulateInputQuery(input);
            if (_searchType == -1)
            {
                Console.WriteLine("Empty string!!");
            }
            else if (_searchType == 1)
            {
                QueryWords(true);
            }
            else if (_searchType == 2)
            {
                PhraseSearch();
            }
            else
            {
                QueryWords(false);
            }
            Console.WriteLine("\n");
        }

        public void PrintResults()
        {
            if (_inputQuery != null)
            {
                Console.Write("\nQuery: " + _inputQuery + "\nMutual docs: ");
                InnerPrint(_searchType == 2 ? _phraseResults : _wordsResults);
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
                _inputQuery = input.Substring(1, input.Length - 2);
                return 2;
            }

            if (input.Split(' ').Length == 1) //one word
            {
                _inputQuery = input;
                return 1;
            }
            else
            {
                _inputQuery = input; // sentence
                return 3;
            }
        }

        private void PhraseSearch()
        {
            QueryWords(false); //I now have results list ready!
            Console.WriteLine("No of suspected docs:{0}", _wordsResults.Count);
            //Console.ReadKey();
            foreach (var t in _wordsResults)
            {
                if (t != "") //just cautious check!
                {
                    var newPath = Path + t + ".html";
                    if (File.ReadAllText(newPath).Contains(_inputQuery))
                    {
                        _phraseResults.Add(t);
                    }
                }
            }
        }

        //Split the string and stem each word
        private List<string>[] PreprocessQuery()
        {
            char[] delimiters = { };
            var queryWords = _inputQuery.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            _distinctqueryWords = queryWords.Distinct().ToArray();
            for (int i = 0; i < _distinctqueryWords.Length; i++)
                _distinctqueryWords[i] = _stemer.stem(_distinctqueryWords[i].ToLower());

            var allLists = new List<string>[_distinctqueryWords.Length];
            for (int i = 0; i < _distinctqueryWords.Length; i++)
                allLists[i] = new List<string>();

            return allLists;
        }

        // Search for sentence and get each word list
        private List<string>[] SearchForSentence(List<string>[] wordsList)
        {
            for (int i = 0; i < _distinctqueryWords.Length; i++)
            {
                if (WordsDictionary.ContainsKey(_distinctqueryWords[i]))
                {
                    var wordStoring = WordsDictionary[_distinctqueryWords[i]];
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
            var stemmedWord = _stemer.stem(_inputQuery.ToLower()); //Preprocess word
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
                    if (t.Count>0)
                    {
                        _wordsResults = t;
                        break;
                    }
                }
                for (int i = 1; i < wordsList.Length; i++)
                {
                    if (wordsList[i].Count > 0)
                    {
                        _wordsResults = _wordsResults.Intersect(wordsList[i]).ToList();
                    }
                }
            }
            else
            {
                _wordsResults = SearchForWord();
            }
        }
    }
}


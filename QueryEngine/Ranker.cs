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
        private readonly List<Dictionary<string, List<string>>> _tagDocsIDsList;
        private readonly Dictionary<string, int> _hrefsReferences;
        private readonly Dictionary<string, string> _docIdUrl;
        private readonly Dictionary<string, string> _urlDocId;
        private Dictionary<string, int> _docIdReferences;
        private readonly UrlPopularityContext _urlPopularityContext = new UrlPopularityContext();

        public Dictionary<string,string> UrlTitleFinalResults { get; set; }

        public Ranker(Search fetcher)
        {
            _fetcher = fetcher;
            _tagDocsIDsList = new List<Dictionary<string, List<string>>>();
            _hrefsReferences = new Dictionary<string, int>();
            _docIdUrl = new Dictionary<string, string>();
            _urlDocId = new Dictionary<string, string>();
            _docIdReferences = new Dictionary<string, int>();
            UrlTitleFinalResults=new Dictionary<string, string>();
        }

        // Sorts the docs IDs according to relevance and popularity
        public void Rank()
        {
            if (_fetcher.SearchType == 1)
                RankWord();
            else if (_fetcher.SearchType == 3)
                RankSentence();
            PopularityRanking();
        }

        private void SplitWordInfo(string wordStoring, int i)
        {
            var word_doc = wordStoring.Split(',');
            for (var j = 0; j < word_doc.Length - 1; j++)
            {
                var docId_tag_occ = word_doc[j].Split('$');
                if (_tagDocsIDsList[i].ContainsKey(docId_tag_occ[1]))
                {
                    _tagDocsIDsList[i][docId_tag_occ[1]].Add(docId_tag_occ[0]);
                }
                else
                {
                    _tagDocsIDsList[i].Add(docId_tag_occ[1], new List<string>() { docId_tag_occ[0] });
                }
            }
        }

        private void RankWord()
        {
            _tagDocsIDsList.Add(new Dictionary<string, List<string>>());
            SplitWordInfo(_fetcher.QueryWordInfo.WordStorings, 0);
        }

        private void RankSentence()
        {
            for (var i = 0; i < _fetcher.DistinctqueryWords.Length; i++)
            {
                var word = _fetcher.DistinctqueryWords[i];
                _tagDocsIDsList.Add(new Dictionary<string, List<string>>());
                if (_fetcher.WordsDictionary.ContainsKey(word))
                {
                    SplitWordInfo(_fetcher.WordsDictionary[word], i);
                }
            }
        }

        private void PopularityRanking()
        {
            if (!_urlPopularityContext.PopularityList.Any())
            {
                ManipulateDocs(_fetcher.Path);
                FillUrlPopularityDb();
            }
            else
            {
                MapLinkId(_fetcher.Path);
                _docIdReferences = _urlPopularityContext.PopularityList.ToDictionary
                    (t => t.Id.ToString(),t => t.ReferencesNumber);
            }
        }

        private void ManipulateDocs(string path)
        {
            MapLinkId(path); // Map each link to its docID
            for (int i = 1; i <= _docIdUrl.Count; i++)
            {
                var docPath = path + i + ".html";
                //if (!File.Exists(docPath))
                //    continue;
                var doc = new HtmlDocument();
                doc.Load(docPath);

                Console.WriteLine("extracting hrefs from doc " + i);
                ExtractDocHrefs(doc, i.ToString());
            }
        }

        private void MapLinkId(string path)
        {
            var links = File.ReadAllLines(path + "log.txt");
            for (int i = 1; i <= links.Length; i++)
            {
                _docIdUrl.Add(i.ToString(), links[i - 1]);
                _urlDocId.Add(links[i - 1], i.ToString());
            }
        }

        private void ExtractDocHrefs(HtmlDocument doc, string docId)
        {
            var docUrlPartitions = _docIdUrl[docId].Split('/');
            var domain = (docUrlPartitions[0] + "//" + docUrlPartitions[2]);

            // Extract hrefs from doc & Add them to a dictionary that holds number of link occurences 
            var linkk = doc.DocumentNode.SelectNodes("//a[@href]");
            if (linkk != null)
            {
                foreach (var link in linkk)
                {
                    var att = link.Attributes["href"];
                    if (att.Value[0] == '#' || att.Value.Length < 2)
                        continue;
                    string fullhref = "";
                    if (att.Value[0] == '/' && att.Value[1] == '/')
                    {
                        fullhref = "https://" + att.Value.Substring(2);
                    }
                    else
                    {
                        fullhref = att.Value[0] == '/' ? domain + att.Value : att.Value;
                    }
                    if (_urlDocId.ContainsKey(fullhref))
                    {
                        if (_hrefsReferences.ContainsKey(fullhref))
                        {
                            _hrefsReferences[fullhref] = _hrefsReferences[fullhref] + 1;
                        }
                        else
                        {
                            _hrefsReferences.Add(fullhref, 1);
                        }
                    }
                }
            }
        }

        private void FillUrlPopularityDb()
        {
            //Console.WriteLine("\nNo. of links " + _hrefsReferences.Count);
            //Console.ReadKey();
            //int i = 1;
            foreach (var href in _hrefsReferences)
            {
                //Console.WriteLine("Filling Db " + i++);
                var popularity = new UrlPopularity()
                {
                    Id = Int32.Parse(_urlDocId[href.Key]),
                    ReferencesNumber = href.Value,
                    Url = href.Key
                };
                _urlPopularityContext.PopularityList.Add(popularity);
                _urlPopularityContext.SaveChanges();
            }
        }

        private void SortDocsByPopularity(ref List<string> filterdWordsList)
        {
            filterdWordsList.Sort((a, b) => _docIdReferences[a].CompareTo(_docIdReferences[b]));
        }

        private string GetDocTitle(string docId)
        {
            var docPath = _fetcher.Path + docId + ".html";
            //if (!File.Exists(docPath))
            //    return "Default";
            var doc = new HtmlDocument();
            doc.Load(docPath);
            var title= doc.DocumentNode.Descendants("title").FirstOrDefault();
            //return title==null ? "Default":title.InnerText;
            return title?.InnerText;
        }

        public void PrintSortedDocs()
        {
            string[] tags = { "title", "h1", "h2", "h3", "p" };
            var traversedDoc = new HashSet<string>();
            foreach (var tag in tags)
            {
                Console.WriteLine("{0} tag:\n", tag);
                foreach (var wordDictionary in _tagDocsIDsList)
                {
                    if (wordDictionary.ContainsKey(tag))
                    {
                        var filterdWordsList = wordDictionary[tag].Intersect(_fetcher.WordsResults).ToList();
                        SortDocsByPopularity(ref filterdWordsList);
                        if (filterdWordsList.Count > 0)
                        {
                            foreach (var d in filterdWordsList)
                            {
                                if (!traversedDoc.Contains(d))
                                {
                                    Console.Write(d + " | ");
                                    UrlTitleFinalResults.Add(_docIdUrl[d],GetDocTitle(d));
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

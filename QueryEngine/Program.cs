using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboSearch
{
    class Program
    {
        public static WordsContext Db = new WordsContext();
        static void Main(string[] args)
        {
            var fetcher = new Search(Db);
            Console.Write("Enter Query: ");
            fetcher.Query(Console.ReadLine());
            fetcher.PrintResults();

        }
    }
}

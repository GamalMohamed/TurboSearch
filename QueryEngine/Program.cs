using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            var fetcher = new Search();
            Console.Write("Enter Query: ");
            fetcher.Query(Console.ReadLine());
        }
    }
}

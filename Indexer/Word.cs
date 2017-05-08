using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboSearch
{
    public class Word
    {
        public int Id { get; set; }
        public string WordContent { get; set; }
        public string WordStorings { get; set; } //urlIDTagsOccurs

        public void SetAttributes(int num, string word, string info)
        {
            this.Id = num;
            this.WordContent = word;
            this.WordStorings = info;

        }
        public void PrintAttributes()
        {
            Console.WriteLine(this.Id + " " + this.WordContent + " " + this.WordStorings);
        }

        public Word()
        {
            Id = 0;
            WordContent = "";
            WordStorings = "";
        }


    }

    public class WordsContext : DbContext
    {
        public DbSet<Word> Words { get; set; }
    }

}

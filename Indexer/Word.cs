﻿using System;
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
        public string UrlIdTags { get; set; }

        public Word(int id, string str, int idUrl, string tag)
        {
            this.Id = id;
            this.WordContent = str;
            appendID_tag(idUrl, tag);
        }

        public Word()
        {
            Id = 0;
            WordContent = "";
            UrlIdTags = "";
        }

        public void appendID_tag(int id, string tag)
        {
            UrlIdTags += id.ToString() + '$' + tag + ',';
        }

        public bool CheckIDexists(int idurl)
        {
            var words = UrlIdTags.Split(',');
            foreach (var word in words)
            {
                var words2 = word.Split('$');
                if (words2[0] == idurl.ToString())
                    return true;
            }
            return false;
        }

    }

    public class WordContext : DbContext
    {
        public DbSet<Word> Words { get; set; }
    }

}

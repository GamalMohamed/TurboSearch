using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.IO;

namespace TurboSearch
{
    public class UrlPopularity
    {
        public int Id { get; set; }

        public string Url { get; set; }

        public int ReferencesNumber { get; set; }
    }

    public class UrlPopularityContext : DbContext
    {
        public DbSet<UrlPopularity> PopularityList { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;

namespace MT1.AmazonProductAdvtApi.Kindle
{
    public class KindleDbContext : DbContext
    {
        public DbSet<SaleInformation> SaleInformations { get; set; }
        public DbSet<ItemDetail> ItemDetail { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Kindle.db");
        }
    }
}

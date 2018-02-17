using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;

namespace MT1.Kindle.Database
{
    public class KindleDbContext : DbContext
    {
        public string DataSource { get; set; } = "Kindle.db";

        public DbSet<SaleInformation> SaleInformations { get; set; }
        public DbSet<ItemDetail> ItemDetails { get; set; }

        public KindleDbContext()
        {
        }

        public KindleDbContext(string dataSource)
        {
            DataSource = dataSource;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=" + DataSource);
        }
    }
}

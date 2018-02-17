using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Xunit;

using MT1.Kindle.Database;

namespace MT1.Kindle.Tests.Database
{
    public class KindleDbContextFixture : IDisposable
    {
        public string DataSource { get; } = @"InsertTest.db";

        // Setup
        public KindleDbContextFixture()
        {
            // データベースの作成
            using (var db = new KindleDbContext(DataSource))
            {
                db.Database.EnsureCreated();
            }
        }

        // Teardown
        public void Dispose()
        {
            File.Delete(DataSource);
        }
    }

    public class KindleDbContextTest : IClassFixture<KindleDbContextFixture>
    {
        KindleDbContextFixture fixture;

        public KindleDbContextTest(KindleDbContextFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void InsertTest()
        {
            using (var context = new KindleDbContext(fixture.DataSource))
            {
                var saleInformation = new SaleInformation
                {
                    Url = "http://blogs.msdn.com/dotnet",
                    Items = new List<ItemDetail>
                    {
                        new ItemDetail { Title = "Intro to C#" },
                        new ItemDetail { Title = "Intro to VB.NET" },
                        new ItemDetail { Title = "Intro to F#" }
                    }
                };

                context.SaleInformations.Add(saleInformation);
                context.SaveChanges();
            }
        }
    }
}

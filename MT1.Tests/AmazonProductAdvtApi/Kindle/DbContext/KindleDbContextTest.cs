using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

using MT1.AmazonProductAdvtApi.Kindle;

namespace MT1.Tests.AmazonProductAdvtApi.Kindle.DbContext
{
   public class KindleDbContextTest
    {
        [Fact]
        public void InsertTest()
        {
            using (var context = new KindleDbContext())
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

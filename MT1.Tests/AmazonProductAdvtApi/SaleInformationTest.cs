using System;
using Xunit;

using MT1.AmazonProductAdvtApi.Kindle;

namespace MT1.Tests
{
    public class SaleInformationTest
    {
        [Fact]
        public void SetSalePeriodTest()
        {
            var saleInformation = new SaleInformation();

            saleInformation.SetSalePeriod("8/18", "8/31");
            Assert.Equal("2017/08/18", saleInformation.StartDate.ToString("yyyy/MM/dd"));
            Assert.Equal("2017/08/31", saleInformation.EndDate.ToString("yyyy/MM/dd"));

            saleInformation.SetSalePeriod("12/31", "1/1");
            Assert.Equal("2017/12/31", saleInformation.StartDate.ToString("yyyy/MM/dd"));
            Assert.Equal("2018/01/01", saleInformation.EndDate.ToString("yyyy/MM/dd"));
        }

        [Fact]
        public void GetSalePeriodTest()
        {
            var saleInformation = new SaleInformation();

            saleInformation.SaleStarted = false;
            saleInformation.SaleFinished = false;
            Assert.Equal("期間不明", saleInformation.GetSalePeriod());

            saleInformation.SaleStarted = true;
            saleInformation.EndDate = DateTime.Parse("2017/08/31");
            Assert.Equal("8/31まで", saleInformation.GetSalePeriod());

            saleInformation.StartDate = DateTime.Parse("2017/08/01");
            Assert.Equal("8/1～8/31", saleInformation.GetSalePeriod());

            saleInformation.SaleFinished = true;
            Assert.Equal("終了", saleInformation.GetSalePeriod());
        }
    }
}

using System;
using Xunit;

using MT1.AmazonProductAdvtApi.Kindle;

namespace MT1.Test
{
    public class SaleInformationTest
    {
        [Fact]
        public void SetSalePeriodTest()
        {
            var saleInformation = new Kindle.SaleInformation();

            saleInformation.SetSalePeriod("8/18", "8/31");
            Assert.Equal("2017/08/18", saleInformation.StartDate.ToString("yyyy/MM/dd"));
            Assert.Equal("2017/08/31", saleInformation.EndDate.ToString("yyyy/MM/dd"));

            saleInformation.SetSalePeriod("12/31", "1/1");
            Assert.Equal("2017/12/31", saleInformation.StartDate.ToString("yyyy/MM/dd"));
            Assert.Equal("2018/01/01", saleInformation.EndDate.ToString("yyyy/MM/dd"));
        }
    }
}

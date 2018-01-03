using System;
using Xunit;

using MT1.AmazonProductAdvtApi.Kindle;

namespace MT1.Tests
{
    public class SaleInformationTest
    {
        [Theory,
            InlineData(2018, 12, 1, "8/18", "8/31", 50, 100, "2018/08/18", "2018/08/31"),
            InlineData(2018, 12, 1, "12/30", "12/31", 50, 100, "2018/12/30", "2018/12/31"),
            InlineData(2018, 12, 1, "12/31", "1/1", 50, 100, "2018/12/31", "2019/01/01"),
            InlineData(2018, 12, 1, "1/1", "1/8", 50, 100, "2018/01/01", "2018/01/08"),
            // 年跨ぎを考慮する最新のセール情報
            InlineData(2018, 12, 1, "8/18", "8/31", 51, 100, "2018/08/18", "2018/08/31"),
            InlineData(2018, 12, 1, "12/30", "12/31", 51, 100, "2018/12/30", "2018/12/31"),
            InlineData(2018, 12, 1, "12/31", "1/1", 51, 100, "2018/12/31", "2019/01/01"),
            InlineData(2018, 12, 1, "1/1", "1/8", 51, 100, "2019/01/01", "2019/01/08"),
            InlineData(2019, 1, 1, "8/18", "8/31", 51, 100, "2019/08/18", "2019/08/31"),
            InlineData(2019, 1, 1, "12/30", "12/31", 51, 100, "2018/12/30", "2018/12/31"),
            InlineData(2019, 1, 1, "12/31", "1/1", 51, 100, "2018/12/31", "2019/01/01"),
            InlineData(2019, 1, 1, "1/1", "1/8", 51, 100, "2019/01/01", "2019/01/08")]
        public void SetSalePeriodTest(int nowYear, int nowMonth, int nowDay, string startDate, string endDate, int count, int total, string expectedStartDate, string expectedEndDate)
        {
            var saleInformation = new SaleInformation();

            DateTime now = new DateTime(nowYear, nowMonth, nowDay);

            saleInformation.SetSalePeriod(now, startDate, endDate, count, total);
            Assert.Equal(expectedStartDate, saleInformation.StartDate.ToString("yyyy/MM/dd"));
            Assert.Equal(expectedEndDate, saleInformation.EndDate.ToString("yyyy/MM/dd"));
        }

        [Fact]
        public void GetSalePeriodTest()
        {
            var saleInformation = new SaleInformation();

            saleInformation.SaleStarted = false;
            saleInformation.SaleFinished = false;
            Assert.Equal("不明", saleInformation.GetSalePeriod());

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

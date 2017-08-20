using System;
using System.Collections.Generic;
using System.Text;

using MT1.GoogleApi;

namespace MT1.AmazonProductAdvtApi.Kindle
{
    partial class Kindle
    {
        public class SaleInformation
        {
            public string NodeId = null;
            public string Name = null;
            public bool Error = false;
            // セール開始日
            public DateTime StartDate;
            // セール終了日
            public DateTime EndDate;
            public string TotalResults = null;
            public List<ItemDetail> Items = null;
            public PostInformation PostInformation = null;

            public void SetSalePeriod(string StartDate, string EndDate)
            {
                // /で分割して配列に格納する
                var StartDates = StartDate.Split('/');
                var EndDates = EndDate.Split('/');

                this.StartDate = new DateTime(DateTime.Now.Year, int.Parse(StartDates[0]), int.Parse(StartDates[1]));
                this.EndDate = new DateTime(DateTime.Now.Year, int.Parse(EndDates[0]), int.Parse(EndDates[1]));

                // 開始日が終了日より未来の場合は年をまたいでいると判断し、終了日の年を進める
                if (this.StartDate > this.EndDate)
                {
                    this.EndDate = this.EndDate.AddYears(1);
                }
            }
        }
    }
}

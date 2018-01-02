using System;
using System.Collections.Generic;
using System.Text;

using MT1.GoogleApi;

namespace MT1.AmazonProductAdvtApi.Kindle
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
        // セールが開始したかどうか
        public bool SaleStarted = false;
        // セールが終了したかどうか
        public bool SaleFinished = false;
        public int TotalResults = 0;
        public List<ItemDetail> Items = new List<ItemDetail>();
        public PostInformation PostInformation = null;

        public void SetSalePeriod(DateTime now, string StartDate, string EndDate, int count, int total)
        {
            // /で分割して配列に格納する
            if (StartDate != null)
            {
                var StartDates = StartDate.Split('/');
                this.StartDate = new DateTime(now.Year, int.Parse(StartDates[0]), int.Parse(StartDates[1]));
            }

            if (EndDate != null)
            {
                var EndDates = EndDate.Split('/');
                this.EndDate = new DateTime(now.Year, int.Parse(EndDates[0]), int.Parse(EndDates[1]));
            }

            // 開始日が終了日より未来の場合は年をまたいでいると判断し、終了日の年を進める
            if (StartDate != null)
            {
                if (this.StartDate > this.EndDate)
                {
                    this.EndDate = this.EndDate.AddYears(1);
                }
            }

            // 最新の一定件数のセール情報において
            if (total - count < 50)
            {
                if (DateTime.Now.Year == this.EndDate.Year)
                {
                    // 現在が 12月で、セール終了日が同年の 1月 であれば、終了日の年を進める
                    if (now.Month == 12 && this.EndDate.Month == 1)
                    {
                        this.EndDate = this.EndDate.AddYears(1);
                        if (this.StartDate.Month == 1)
                        {
                            this.StartDate = this.StartDate.AddYears(1);
                        }
                    }
                    // 現在が 1月で、セール終了日が同年の 12月 であれば、終了日の年を戻す
                    else if (now.Month == 1 && this.EndDate.Month == 12)
                    {
                        this.EndDate = this.EndDate.AddYears(-1);
                        if (this.StartDate.Year > this.EndDate.Year)
                        {
                            this.StartDate.AddYears(-1);
                        }
                    }
                }
            }
        }

        public string GetSalePeriod()
        {
            if (SaleFinished == true)
            {
                return "終了";
            }

            if (SaleStarted == true && SaleFinished == false)
            {
                if (StartDate == DateTime.MinValue)
                {
                    return $"{EndDate.ToString("M/d")}まで";
                }
                else
                {
                    return $"{StartDate.ToString("M/d")}～{EndDate.ToString("M/d")}";
                }
            }

            return "不明";
        }
    }
}

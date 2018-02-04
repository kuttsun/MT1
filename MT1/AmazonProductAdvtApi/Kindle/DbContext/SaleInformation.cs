using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using MT1.GoogleApi;

namespace MT1.AmazonProductAdvtApi.Kindle
{
    public class SaleInformation
    {
        [Key]
        public string NodeId { get; set; } = null;
        public string Name { get; set; } = null;
        public bool Error { get; set; } = false;
        // セール開始日
        public DateTime StartDate { get; set; }
        // セール終了日
        public DateTime EndDate { get; set; }
        // セールが開始したかどうか
        public bool SaleStarted { get; set; } = false;
        // セールが終了したかどうか
        public bool SaleFinished { get; set; } = false;
        public int TotalResults { get; set; } = 0;
        public string Url { get; set; }
        public string PostId { get; set; }
        public DateTime? Published { get; set; }
        public List<ItemDetail> Items { get; set; } = new List<ItemDetail>();

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

            // 開始日が終了日より未来の場合は年をまたいでいると判断
            if (StartDate != null)
            {
                if (this.StartDate > this.EndDate)
                {
                    if (now.Month == 1)
                    {
                        this.StartDate = this.StartDate.AddYears(-1);
                    }
                    else
                    {
                        this.EndDate = this.EndDate.AddYears(1);
                    }
                }
            }

            // 最新の一定件数のセール情報において
            if (total - count < 50)
            {
                if (now.Year == this.EndDate.Year)
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
                            this.StartDate = this.StartDate.AddYears(-1);
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

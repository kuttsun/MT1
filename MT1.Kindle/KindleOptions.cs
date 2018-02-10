using System;
using System.Collections.Generic;
using System.Text;

namespace MT1.Kindle
{
    // json から読み込んだ設定を格納するデータクラス
    // メンバーはプロパティかつ public の必要あり

    public class KindleOptions
    {
        public string BlogId { get; set; }
        public string CurrentSaleListPageId { get; set; }
        public string LatestSaleListPageId { get; set; }
        public int ActiveCount { get; set; }
    }
}

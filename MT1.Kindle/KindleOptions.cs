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
        public string OutDir { get; set; }
        public string NodeListFile { get; set; }
        public string DataFile { get; set; }
        public int ActiveCount { get; set; }
        public Debug Debug { get; set; }

        public string GetNodeListFilePath()
        {
            return OutDir + NodeListFile;
        }

        public string GetDataFilePath()
        {
            return OutDir + DataFile;
        }
    }

    public class Debug
    {
        public int NumberOfNodesToGet { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MT1.Options
{
    // json から読み込んだ設定を格納するデータクラス
    // メンバーはプロパティかつ public の必要あり

    public class KindleOptions
    {
        public string BlogId { get; set; }
        public string PageId { get; set; }
        public string NodeListFile { get; set; }
        public string DataFile { get; set; }
        public Debug Debug { get; set; }
    }

    public class Debug
    {
        public int NumberOfNodesToGet { get; set; }
    }
}

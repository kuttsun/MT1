using System;
using System.Collections.Generic;
using System.Text;

namespace MT1
{
    // json から読み込んだ設定を格納するデータクラス
    // メンバーはプロパティかつ public の必要あり

    public class AmazonOptions
    {
        public string AccessKeyId { get; set; }
        public string SecretKey { get; set; }
        public string AssociateTag { get; set; }
    }

    public class KindleOptions
    {
        public string BlogId { get; set; }
        public string PageId { get; set; }
        public string NodeListFile { get; set; }
        public string DataFile { get; set; }
    }
}

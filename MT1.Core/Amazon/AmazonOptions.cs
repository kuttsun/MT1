using System;
using System.Collections.Generic;
using System.Text;

namespace MT1.Core.Amazon
{
    // json から読み込んだ設定を格納するデータクラス
    // メンバーはプロパティかつ public の必要あり

    public class AmazonOptions
    {
        public string AccessKeyId { get; set; }
        public string SecretKey { get; set; }
        public string AssociateTag { get; set; }
    }
}

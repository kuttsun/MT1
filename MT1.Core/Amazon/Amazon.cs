using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MT1.Core.Amazon
{
    public class Amazon
    {
        protected HttpClient client = new HttpClient();
        protected readonly int requestWaitTimerMSec = 1500;

        protected static readonly string service = "AWSECommerceService";
        protected static readonly string apiVersion = "2011-08-01";
        protected static readonly string ns = "http://webservices.amazon.com/AWSECommerceService/2011-08-01";

        SignedRequestHelper helper;
        ILogger logger;
        AmazonOptions options;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Amazon(ILogger<Amazon> logger, IOptions<AmazonOptions> amazonOptions)
        {
            this.logger = logger;
            options = amazonOptions?.Value;

            string destination = "ecs.amazonaws.jp";

            if (options != null)
            {
                helper = new SignedRequestHelper(options.AccessKeyId, options.SecretKey, destination, options.AssociateTag);
            }

            // タイムアウトをセット
            client.Timeout = TimeSpan.FromSeconds(10.0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected Stream GetXml(IDictionary<string, string> request)
        {
            // 署名を行う
            var requestUrl = helper.Sign(request);

            // 参考：http://www.atmarkit.co.jp/ait/articles/1501/06/news086.html
            while (true)
            {
                try
                {
                    return client.GetStreamAsync(requestUrl).Result;
                }
                catch (Exception e)
                {
                    logger.LogError("取得失敗、リトライします\n" + e.Message);
                    Task.Delay(requestWaitTimerMSec).Wait();
                }
            }
        }

        /// <summary>
        /// XMLファイルに出力する
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="outputFile"></param>
        protected void WriteXml(XmlDocument doc, string outputFile)
        {
            try
            {
                using (var fileStream = new FileStream(outputFile, FileMode.Create))
                {
                    doc.Save(fileStream);
                }
            }
            catch (Exception e)
            {
                logger.LogError($"{outputFile} 出力失敗\n{e.Message}");
            }
        }

        /// <summary>
        /// ブラウズノードのアソシエイトリンクを取得
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// 参考：https://affiliate.amazon.co.jp/help/topic/t121/a1
        protected string GetAssociateLinkByBrowseNode(string node)
        {
            return $"http://www.amazon.co.jp/b?ie=UTF8&node={node}&tag={options.AssociateTag}&linkCode=ure&creative=6339";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

namespace MT1.AmazonProductAdvtApi
{
    public class Amazon
    {
        HttpClient client = new HttpClient();

        protected static readonly string service = "AWSECommerceService";
        protected static readonly string apiVersion = "2011-08-01";
        protected static readonly string ns = "http://webservices.amazon.com/AWSECommerceService/2011-08-01";

        SignedRequestHelper helper;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Amazon()
        {
            string awsAccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            string awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");
            string associateTag = Environment.GetEnvironmentVariable("ASSOCIATE_TAG");
            string destination = "ecs.amazonaws.jp";

            helper = new SignedRequestHelper(awsAccessKeyId, awsSecretKey, destination, associateTag);

            // タイムアウトをセット
            client.Timeout = TimeSpan.FromSeconds(10.0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected async Task<Stream> GetXmlAsync(IDictionary<string, string> request)
        {
            // 署名を行う
            var requestUrl = helper.Sign(request);

            // 参考：http://www.atmarkit.co.jp/ait/articles/1501/06/news086.html
            while (true)
            {
                try
                {
                    // Webページを取得するのは、事実上この1行だけ
                    return await client.GetStreamAsync(requestUrl);
                }
                catch (HttpRequestException e)
                {
                    // 404エラーや、名前解決失敗など
                    Console.WriteLine("\n例外発生!");
                    // InnerExceptionも含めて、再帰的に例外メッセージを表示する
                    Exception ex = e;
                    while (ex != null)
                    {
                        Console.WriteLine(ex.Message);
                        ex = ex.InnerException;
                    }

                    //throw;
                }
                catch (TaskCanceledException e)
                {
                    // タスクがキャンセルされたとき（一般的にタイムアウト）
                    Console.WriteLine("\nタイムアウト!");
                    Console.WriteLine(e.Message);

                    // throw;
                }

                await Task.Delay(2000);
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
                Console.WriteLine(e.Message);
            }
        }
    }
}

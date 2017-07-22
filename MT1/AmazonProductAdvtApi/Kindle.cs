using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using System.Xml;
using System.Xml.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

using System.Threading;
using System.Xml.Serialization;

using MT1.GoogleApi;

namespace MT1.AmazonProductAdvtApi
{
    public class Kindle
    {
        [XmlIgnore]
        const string service = "AWSECommerceService";
        [XmlIgnore]
        const string apiVersion = "2010-09-01";
        [XmlIgnore]
        const string ns = "http://webservices.amazon.com/AWSECommerceService/2011-08-01";

        [XmlIgnore]
        const string NodeListXml = "KindleNodeList.xml";
        [XmlIgnore]
        const string SaleInformationsXml = "KindleSaleInformations.xml";

        [XmlIgnore]
        SignedRequestHelper helper;
        [XmlIgnore]
        Blogger blogger;
        [XmlIgnore]
        Timer timer;

        public class ItemDetail
        {
            public string asin = null;
            public string detailPageUrl = null;
            public string imageUrl = null;
        }

        public class SaleInformation
        {
            public string nodeId = null;
            public string name = null;
            public bool error = false;
            public string moreSearchResultsUrl = null;
            public List<ItemDetail> items;
        }

        public List<SaleInformation> saleInformations = new List<SaleInformation>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Kindle()
        {
            string awsAccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            string awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");
            string associateTag = Environment.GetEnvironmentVariable("ASSOCIATE_TAG");
            string destination = "ecs.amazonaws.jp";

            helper = new SignedRequestHelper(awsAccessKeyId, awsSecretKey, destination, associateTag);

            blogger = new Blogger(Environment.GetEnvironmentVariable("BLOGGER_ID_KINDLE"));

            // タイマーの生成(第３引数の時間経過後から第４引数の時間間隔でコールされる)
            //timer = new Timer(new TimerCallback(GetSaleInformations), null, 60 * 1000, 300 * 1000);
        }

        /// <summary>
        /// コールバック
        /// </summary>
        /// <param name="args"></param>
        public void GetSaleInformations(object args)
        {
            try
            {
                // セールの一覧を取得
                BrowseNodeLookup("2275277051");

                Thread.Sleep(2000);

                // 個々の URL を取得
                int count = 0;
                foreach (var saleInformation in saleInformations)
                {

                    ItemSearch(saleInformation);
                    count++;
                    Console.WriteLine($"[{count}/{saleInformations.Count()}件完了]");
                    Thread.Sleep(2000);

                    if (count >= 10) break;
                }

                SerializeMyself(SaleInformationsXml);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 自分自身をシリアライズする
        /// </summary>
        void SerializeMyself(string filePath)
        {
            var xmlSerializer = new XmlSerializer(typeof(Kindle));
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                xmlSerializer.Serialize(fileStream, this);
            }
        }

        /// <summary>
        /// セールの一覧を取得
        /// </summary>
        /// <param name="nodeId">基準となるノードID</param>
        void BrowseNodeLookup(string browseNodeId)
        {
            IDictionary<string, string> request = new Dictionary<string, String>();
            request["Service"] = service;
            request["Version"] = apiVersion;
            request["Operation"] = "BrowseNodeLookup";
            request["SearchIndex"] = "KindleStore";
            request["BrowseNodeId"] = browseNodeId;

            Console.WriteLine($"現在の件数:{saleInformations.Count()}件");
            Console.WriteLine($"セール情報一覧取得開始");

            // 署名を行う
            var requestUrl = helper.Sign(request);
            // リクエストを送信して xml を取得
            Task<Stream> webTask = GetXmlAsync(requestUrl);
            // 処理が完了するまで待機する
            webTask.Wait();

            // 取得した XML を読み込む
            XmlDocument doc = new XmlDocument();
            doc.Load(webTask.Result);

            WriteXml(doc, NodeListXml);

            // 名前空間の指定
            XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(doc.NameTable);
            xmlNsManager.AddNamespace("ns", ns);

            // XML をパースしてノードリストを取得
            XmlNodeList nodeList = doc.SelectNodes("ns:BrowseNodeLookupResponse/ns:BrowseNodes/ns:BrowseNode/ns:Children/ns:BrowseNode", xmlNsManager);

            List<SaleInformation> newSaleInformations = new List<SaleInformation>();
            foreach (XmlNode node in nodeList)
            {
                newSaleInformations.Add(new SaleInformation()
                {
                    nodeId = node.SelectSingleNode("ns:BrowseNodeId", xmlNsManager).InnerText,
                    name = node.SelectSingleNode("ns:Name", xmlNsManager).InnerText
                });
            }
            Console.WriteLine($"セール情報一覧取得完了({newSaleInformations.Count()}件)");

            // 現在のリストの項目が最新のリスト中になければ古い情報と判断して削除する
            int deleteCount = 0;
            foreach (var saleInformation in saleInformations)
            {
                var foundItem = newSaleInformations.Find(item => item.nodeId == saleInformation.nodeId);

                if (foundItem == null)
                {
                    saleInformations.Remove(saleInformation);
                    deleteCount++;
                }
            }
            Console.WriteLine($"{deleteCount}件の古いデータを削除(残り{saleInformations.Count()}件)");

            // 最新のリストのうち、現在のリスト中にないものだけ現在のリストに新規に追加する
            int addCount = 0;
            foreach (var newSaleInformation in newSaleInformations)
            {
                var foundItem = saleInformations.Find(item => item.nodeId == newSaleInformation.nodeId);

                if (foundItem == null)
                {
                    saleInformations.Add(newSaleInformation);
                    addCount++;
                }
            }
            Console.WriteLine($"{addCount}件の新規データを追加(計{saleInformations.Count()}件)");

            // test用
            saleInformations.Sort((a, b) => string.Compare(b.nodeId, a.nodeId));

            // SortedSaleInformations = saleInfomations.OrderByDescending(x => x.nodeId);
        }

        /// <summary>
        /// 個別に情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemSearch.html
        void ItemSearch(SaleInformation saleInformation)
        {
            if (saleInformation.moreSearchResultsUrl != null)
            {
                Console.WriteLine($"{saleInformation.nodeId} は既にURL取得済み");
                return;
            }

            IDictionary<string, string> request = new Dictionary<string, String>();
            request["Service"] = service;
            request["Version"] = apiVersion;
            request["Operation"] = "ItemSearch";
            request["SearchIndex"] = "KindleStore";
            request["ResponseGroup"] = "Medium";
            request["BrowseNode"] = saleInformation.nodeId;

            Console.WriteLine($"\n{saleInformation.nodeId} の商品情報取得開始");

            // 署名を行う
            var requestUrl = helper.Sign(request);
            // リクエストを送信して xml を取得
            Task<Stream> webTask = GetXmlAsync(requestUrl);
            // 処理が完了するまで待機する
            webTask.Wait();

            XmlDocument doc = new XmlDocument();
            doc.Load(webTask.Result);

            WriteXml(doc, $"{saleInformation.nodeId}_{saleInformation.name}.xml");

            XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(doc.NameTable);
            xmlNsManager.AddNamespace("ns", ns);

            // エラー情報の取得
            try
            {
                var error = doc.SelectSingleNode("ns:ItemSearchResponse/ns:Items/ns:Request/ns:Errors/ns:Error/ns:Code", xmlNsManager).InnerText;
                Console.WriteLine(error);
                saleInformation.error = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                saleInformation.error = false;
            }

            saleInformation.moreSearchResultsUrl = doc.SelectSingleNode("ns:ItemSearchResponse/ns:Items/ns:MoreSearchResultsUrl", xmlNsManager).InnerText;

            // 商品情報を取得
            XmlNodeList nodeList = doc.SelectNodes("ns:ItemSearchResponse/ns:Items/ns:Item", xmlNsManager);
            saleInformation.items = new List<ItemDetail>();
            foreach (XmlNode node in nodeList)
            {
                saleInformation.items.Add(new ItemDetail()
                {
                    asin = node.SelectSingleNode("ns:ASIN", xmlNsManager).InnerText,
                    detailPageUrl = node.SelectSingleNode("ns:DetailPageURL", xmlNsManager).InnerText,
                    imageUrl = node.SelectSingleNode("ns:MediumImage/ns:URL", xmlNsManager).InnerText
                });

                //ItemLookUp(node.SelectSingleNode("ns:ASIN", xmlNsManager).InnerText);
            }
            Console.WriteLine($"商品情報取得完了({saleInformation.items.Count()}件)");

            Console.WriteLine($"{saleInformation.nodeId} の商品情報取得完了");
        }

        /// <summary>
        /// 個別に情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemLookup.html
        void ItemLookUp(string asin)
        {
            IDictionary<string, string> request = new Dictionary<string, String>();
            request["Service"] = service;
            request["Version"] = apiVersion;
            request["Operation"] = "ItemLookup";
            request["IdType"] = "ASIN";
            request["ItemId"] = asin;
            //request["RelationshipType"] = "Episode";

            Console.WriteLine($"ASIN {asin} の詳細取得開始");

            // 署名を行う
            var requestUrl = helper.Sign(request);
            // リクエストを送信して xml を取得
            Task<Stream> webTask = GetXmlAsync(requestUrl);
            // 処理が完了するまで待機する
            webTask.Wait();

            XmlDocument doc = new XmlDocument();
            doc.Load(webTask.Result);

            WriteXml(doc, $"ASIN_{asin}.xml");

            XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(doc.NameTable);
            xmlNsManager.AddNamespace("ns", ns);

            Console.WriteLine($"ASIN {asin} の詳細取得完了");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        async Task<Stream> GetXmlAsync(string uri)
        {
            // 参考：http://www.atmarkit.co.jp/ait/articles/1501/06/news086.html
            using (HttpClient client = new HttpClient())
            {
                // タイムアウトをセット（オプション）
                client.Timeout = TimeSpan.FromSeconds(10.0);

                while (true)
                {
                    try
                    {
                        // Webページを取得するのは、事実上この1行だけ
                        return await client.GetStreamAsync(uri);
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

                    Thread.Sleep(3000);
                }
            }
        }

        /// <summary>
        /// XMLファイルに出力する
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="outputFile"></param>
        void WriteXml(XmlDocument doc, string outputFile)
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

        /// <summary>
        /// ブログに投稿する
        /// </summary>
        public void PostToBlog()
        {
            foreach (var item in saleInformations)
            {
                string content = $"<p><a href='item.url' target='_href'>{item.moreSearchResultsUrl}</a></p>";
                //blogger.Post(item.name, content, new[] { "Kindle" });
            }
        }
    }
}

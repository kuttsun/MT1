﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.IO;

using System.Threading;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

using AngleSharp.Parser.Html;

using MT1.GoogleApi;
using MT1.Extensions;

namespace MT1.AmazonProductAdvtApi.Kindle
{
    public partial class Kindle : Amazon
    {
        [XmlIgnore]
        const string nodeListXml = "KindleNodeList.xml";
        [XmlIgnore]
        const string saleInformationsXml = "KindleSaleInformations.xml";

        [XmlIgnore]
        Blogger blogger;
        [XmlIgnore]
        string pageId = "5107391980448290602";
        [XmlIgnore]
        Timer timer;

        public string LastUpdate = null;

        public List<SaleInformation> saleInformations = new List<SaleInformation>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Kindle()
        {
            blogger = new Blogger(Environment.GetEnvironmentVariable("BLOGGER_ID_KINDLE"));

            // タイマーの生成(第３引数の時間経過後から第４引数の時間間隔でコールされる)
            //timer = new Timer(new TimerCallback(GetSaleInformations), null, 60 * 1000, 300 * 1000);
        }

        /// <summary>
        /// コールバック
        /// </summary>
        /// <param name="args"></param>
        public async void GetSaleInformations(object args)
        {
            try
            {
                // セールの一覧を取得
                await BrowseNodeLookupAsync("2275277051");

                LastUpdate = DateTime.Now.ToString("yyyy/MM/dd (ddd) HH:mm:ss");

                // 個々の URL を取得
                int count = 0;
                foreach (var saleInformation in saleInformations)
                {

                    if (await ItemSearchAsync(saleInformation) == true)
                    {
                        await CheckSalePeriod(saleInformation);

                        await PostToBlogAsync(saleInformation);
                    }
                    count++;
                    Console.WriteLine($"[{count}/{saleInformations.Count()}件完了]");

                    // デバッグ用に指定回数だけ実行する
                    if (count >= 50) break;
                }

                SerializeMyself(saleInformationsXml);

                // セール一覧を更新
                await UpdatePageAsync();
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
        async Task BrowseNodeLookupAsync(string browseNodeId)
        {
            IDictionary<string, string> request = new Dictionary<string, String>();
            request["Service"] = service;
            request["Version"] = apiVersion;
            request["Operation"] = "BrowseNodeLookup";
            request["SearchIndex"] = "KindleStore";
            request["BrowseNodeId"] = browseNodeId;

            Console.WriteLine($"現在の件数:{saleInformations.Count()}件");
            Console.WriteLine($"セール情報一覧取得開始");

            // リクエストを送信して xml を取得
            var result = await GetXmlAsync(request);

            // 取得した XML を読み込む
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            WriteXml(doc, nodeListXml);

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
                    NodeId = node.SelectSingleNode("ns:BrowseNodeId", xmlNsManager).InnerText,
                    Name = node.SelectSingleNode("ns:Name", xmlNsManager).InnerText
                });
            }
            Console.WriteLine($"セール情報一覧取得完了({newSaleInformations.Count()}件)");

            // 現在のリストの項目が最新のリスト中になければ古い情報と判断して削除する
            int deleteCount = 0;
            foreach (var saleInformation in saleInformations)
            {
                var foundItem = newSaleInformations.Find(item => item.NodeId == saleInformation.NodeId);

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
                var foundItem = saleInformations.Find(item => item.NodeId == newSaleInformation.NodeId);

                if (foundItem == null)
                {
                    saleInformations.Add(newSaleInformation);
                    addCount++;
                }
            }
            Console.WriteLine($"{addCount}件の新規データを追加(計{saleInformations.Count()}件)");

            // test用
            saleInformations.Sort((a, b) => string.Compare(b.NodeId, a.NodeId));

            // SortedSaleInformations = saleInfomations.OrderByDescending(x => x.nodeId);
        }

        /// <summary>
        /// 個別に情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemSearch.html
        async Task<bool> ItemSearchAsync(SaleInformation saleInformation)
        {
            if (saleInformation.PostInformation != null)
            {
                Console.WriteLine($"{saleInformation.NodeId} は既に投稿済み");
                return false;
            }

            await Task.Delay(2000);

            IDictionary<string, string> request = new Dictionary<string, String>();
            request["Service"] = service;
            request["Version"] = apiVersion;
            request["Operation"] = "ItemSearch";
            request["SearchIndex"] = "KindleStore";
            request["ResponseGroup"] = "Medium";
            request["BrowseNode"] = saleInformation.NodeId;

            Console.WriteLine($"\n{saleInformation.NodeId} の商品情報取得開始");

            // リクエストを送信して xml を取得
            var result = await GetXmlAsync(request);

            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            WriteXml(doc, $"{saleInformation.NodeId}.xml");

            XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(doc.NameTable);
            xmlNsManager.AddNamespace("ns", ns);

            // エラー情報の取得
            try
            {
                var error = doc.SelectSingleNode("ns:ItemSearchResponse/ns:Items/ns:Request/ns:Errors/ns:Error/ns:Code", xmlNsManager).InnerText;
                Console.WriteLine("エラー情報あり：" + error);
                saleInformation.Error = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                saleInformation.Error = false;
            }

            saleInformation.TotalResults = doc.SelectSingleNode("ns:ItemSearchResponse/ns:Items/ns:TotalResults", xmlNsManager).InnerText;

            // 商品情報を取得
            XmlNodeList nodeList = doc.SelectNodes("ns:ItemSearchResponse/ns:Items/ns:Item", xmlNsManager);
            saleInformation.Items = new List<ItemDetail>();
            foreach (XmlNode node in nodeList)
            {
                saleInformation.Items.Add(new ItemDetail()
                {
                    Title = node.SelectSingleNode("ns:ItemAttributes/ns:Title", xmlNsManager).InnerText,
                    PublicationDate = node.SelectSingleNode("ns:ItemAttributes/ns:PublicationDate", xmlNsManager)?.InnerText,
                    Content = node.SelectSingleNode("ns:EditorialReviews/ns:EditorialReview/ns:Content", xmlNsManager)?.InnerText,
                    Asin = node.SelectSingleNode("ns:ASIN", xmlNsManager).InnerText,
                    DetailPageUrl = node.SelectSingleNode("ns:DetailPageURL", xmlNsManager).InnerText,
                    MediumImageUrl = node.SelectSingleNode("ns:MediumImage/ns:URL", xmlNsManager).InnerText,
                    LargeImageUrl = node.SelectSingleNode("ns:LargeImage/ns:URL", xmlNsManager).InnerText
                });

                //ItemLookUp(node.SelectSingleNode("ns:ASIN", xmlNsManager).InnerText);
            }
            Console.WriteLine($"商品情報取得完了({saleInformation.Items.Count()}件)");

            Console.WriteLine($"{saleInformation.NodeId} の商品情報取得完了");

            return true;
        }

        /// <summary>
        /// 個別に情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemLookup.html
        async Task ItemLookUpAsync(string asin)
        {
            IDictionary<string, string> request = new Dictionary<string, String>();
            request["Service"] = service;
            request["Version"] = apiVersion;
            request["Operation"] = "ItemLookup";
            request["IdType"] = "ASIN";
            request["ItemId"] = asin;
            //request["RelationshipType"] = "Episode";

            Console.WriteLine($"ASIN {asin} の詳細取得開始");

            // リクエストを送信して xml を取得
            var result = await GetXmlAsync(request);

            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            WriteXml(doc, $"ASIN_{asin}.xml");

            XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(doc.NameTable);
            xmlNsManager.AddNamespace("ns", ns);

            Console.WriteLine($"ASIN {asin} の詳細取得完了");
        }

        /// <summary>
        /// ブログに投稿する
        /// </summary>
        async Task PostToBlogAsync(SaleInformation saleInformation)
        {
            if (saleInformation.Error == true)
            {
                return;
            }

            saleInformation.PostInformation = await blogger.PostAsync(CreateArticle(saleInformation));

            Console.WriteLine($"{saleInformation.PostInformation.Url}\n{saleInformation.PostInformation.PostId}");
        }

        /// <summary>
        /// 投稿済みの記事を更新する
        /// </summary>
        async Task UpdateArticleAsync(SaleInformation saleInformation)
        {

            await blogger.UpdatePostAsync(CreateArticle(saleInformation), saleInformation.PostInformation);

            Console.WriteLine($"{saleInformation.PostInformation.Url}\n{saleInformation.PostInformation.PostId}");
        }

        /// <summary>
        /// 記事の内容を作成する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// <returns></returns>
        Article CreateArticle(SaleInformation saleInformation)
        {
            var article = new Article();

            article.title = saleInformation.Name;

            article.content += $@"<p>
            対象は{saleInformation.TotalResults}冊。<br>
            <a href='{GetAssociateLinkByBrowseNode(saleInformation.NodeId)}' target='_blank'>セールページはこちら</a>。
            </p>";

            foreach (var item in saleInformation.Items)
            {
                article.content += $@"<p>
            <a href='{item.DetailPageUrl}' target='_href'><img src='{item.MediumImageUrl}' /></a>
            <a href='{item.DetailPageUrl}' target='_href'>{item.Title}</a>
            {item.Asin} {item.PublicationDate}<br>{item.Content}
            </p>";
            }

            // タイトルからラベルを抽出
            article.labels = ExtractLabels(article.title);

            return article;
        }

        /// <summary>
        /// タイトルからタグを抽出する
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public List<string> ExtractLabels(string title)
        {
            var labels = new List<string>();

            var pairs = new Dictionary<string, string[]>();

            pairs.Add("割引", new[] { "%OFF" });
            pairs.Add("ポイント還元", new[] { "ポイント還元" });
            pairs.Add("キャンペーン", new[] { "キャンペーン" });
            pairs.Add("期間限定", new[] { "期間限定" });
            pairs.Add("無料", new[] { "無料" });
            pairs.Add("均一", new[] { "均一" });
            pairs.Add("特集・フェア", new[] { "特集", "フェア" });

            foreach (var key in pairs.Keys)
            {
                if (title.Contains(pairs[key]) == true)
                {
                    labels.Add(key);
                }
            }

            return labels;
        }

        /// <summary>
        /// タイトルから終了日を抽出
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        /// https://www.ipentec.com/document/document.aspx?page=csharp-regular-expression-patern-match-and-extract-substring
        /// http://kuroeveryday.blogspot.jp/2014/10/regex.html
        public string ExtractEndDate(string title)
        {
            //var result = Regex.Match(title, ".*\\((?<EndDate>.*?)まで\\).*");
            var result = Regex.Match(title, @".*(\(|（)(?<EndDate>.*?)まで(\)|）).*");

            if (result.Success == true)
            {
                return result.Groups["EndDate"].Value;
            }

            return null;
        }

        /// <summary>
        /// セールページをスクレイピングしてセール期間を判別
        /// </summary>
        /// <param name="saleInformation"></param>
        /// <returns></returns>
        public async Task<bool> ExtractSalePeriod(SaleInformation saleInformation)
        {
            // 指定したサイトのHTMLをストリームで取得する
            using (var stream = await client.GetStreamAsync(GetAssociateLinkByBrowseNode(saleInformation.NodeId)))
            {
                // AngleSharp.Parser.Html.HtmlParserオブジェクトにHTMLをパースさせる
                var parser = new HtmlParser();
                var doc = parser.Parse(stream);

                // ページによって h3 の場合も h4 の場合もある
                foreach (var tag in new[] { "h3", "h4" })
                {
                    var elements = doc.QuerySelectorAll(tag);

                    foreach (var element in elements)
                    {
                        // 例：期間限定：8/18（金）～8/31（木）
                        var result = Regex.Match(element.InnerHtml, @"期間限定：(?<StartDate>.*?)（.*）～(?<EndDate>.*?)（.*）");

                        if (result.Success == true)
                        {
                            saleInformation.SetSalePeriod(result.Groups["StartDate"].Value, result.Groups["EndDate"].Value);
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        async Task UpdatePageAsync()
        {
            string content = $@"<p>
            Amazon Product Advertising API から取得した Kindle のセールページ一覧です。<br>
            この中にはまだセールを開始していないものや、既に終了したセールなど、セールではないものもありますのでご注意ください。
            </p>
            <p>
            更新日時：{LastUpdate}
            </p>";

            int count = 0;
            content += @"<table class=""marked"">
            <tr><th>No</th><th>開催期間</th><th>タイトル</th><th>エラー</td><th>開催</th><th>終了</th></tr>";
            foreach (var saleInformation in saleInformations)
            {
                content += $@"<tr>
                <td>{count++}</td>
                <td>{saleInformation.GetSalePeriod()}</td>
                <td><a href='{GetAssociateLinkByBrowseNode(saleInformation.NodeId)}' target='_blank'>{saleInformation.Name}</a></td>
                <td>{saleInformation.Error}</td>
                <td>{saleInformation.SaleStarted}</td>
                <td>{saleInformation.SaleFinished}</td>
                </tr>";
            }
            content += "</table>";

            await blogger.UpdatePageAsync(pageId, content);
        }

        /// <summary>
        /// セール期間を判別してセットする
        /// </summary>
        /// <param name="saleInformation"></param>
        async Task CheckSalePeriod(SaleInformation saleInformation)
        {
            // タイトルから終了日を判別
            var endDate = ExtractEndDate(saleInformation.Name);
            if (endDate != null)
            {
                saleInformation.EndDate = DateTime.Parse(endDate);

                if (saleInformation.EndDate < DateTime.Now)
                {
                    saleInformation.SaleFinished = true;
                }
                else
                {
                    saleInformation.SaleStarted = true;
                }
                return;
            }

            // セールページをスクレイピングしてセール期間を判別
            if (await ExtractSalePeriod(saleInformation) == true)
            {
                if (saleInformation.EndDate < DateTime.Now)
                {
                    saleInformation.SaleFinished = true;
                }
                else if (saleInformation.StartDate < DateTime.Now)
                {
                    saleInformation.SaleStarted = true;
                    saleInformation.SaleFinished = false;
                }
                return;
            }

            // ここまできたら、セールページは存在するが、タイトルからもセールページからもセール期間が判別できないので、
            // 何も処理しない
        }
    }
}

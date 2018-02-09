using System;
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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

using AngleSharp.Parser.Html;

using MT1.Core.Amazon;
using MT1.Core.Google.Blogger;
using MT1.Core.Extensions;

using MT1.Kindle.Database;

namespace MT1.Kindle
{
    public partial class Kindle : Amazon
    {
        ILogger logger;
        IBlogger blogger;
        //KindleData data;
        KindleOptions options;
        DateTime lastUpdate;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Kindle(IBlogger blogger, ILogger<Kindle> logger, IOptions<KindleOptions> kindleOptions, IOptions<AmazonOptions> amazonOptions) : base(logger, amazonOptions)
        {
            this.logger = logger;
            options = kindleOptions?.Value;
            this.blogger = blogger;

            if (this.blogger != null)
            {
                this.blogger.BlogId = options?.BlogId;
            }

            // データベースの作成
            using (var db = new KindleDbContext())
            {
                db.Database.EnsureCreated();
            }

            //var serializer = new XmlSerializer(typeof(KindleData));
            //try
            //{
            //    using (var fs = new FileStream(options.GetDataFilePath(), FileMode.Open))
            //    {
            //        data = (KindleData)serializer.Deserialize(fs);
            //    }
            //    logger.LogInformation("デシリアライズ完了");
            //}
            //catch
            //{
            //    logger.LogError("デシリアライズ失敗");
            //    data = new KindleData();
            //}
        }

        /// <summary>
        /// 処理の開始
        /// </summary>
        /// <param name="args"></param>
        public void Run()
        {
            logger?.LogInformation("----- Begin -----");

            try
            {
                // セールの一覧を取得
                BrowseNodeLookup("2275277051");
                Task.Delay(requestWaitTimerMSec).Wait();

                lastUpdate = DateTime.Now;//. ToString("yyyy/MM/dd (ddd) HH:mm:ss");

                // 個々の URL を取得
                int count = 0;
                using (var context = new KindleDbContext())
                {
                    var saleInformations = context.SaleInformations
                        .Include(SaleInformation => SaleInformation.Items)
                        .ToList();

                    int total = saleInformations.Count();

                    foreach (var saleInformation in saleInformations)
                    {
                        logger?.LogInformation($"[{count}/{total}件開始]");

                        if ((total - options.ActiveCount) <= count && count < total)
                        {
                            // 最新の数件のみ処理

                            // 既にセールが終了していないかどうかチェック
                            if (saleInformation.SaleFinished == true)
                            {
                                logger?.LogInformation($"{saleInformation.NodeId} は既にセール終了");
                            }
                            else
                            {
                                // ブログに投稿済みかどうかチェック
                                if (saleInformation.Url != null)
                                {
                                    try
                                    {
                                        logger?.LogInformation($"{saleInformation.NodeId} は既に投稿済み");

                                        CheckSalePeriod(saleInformation, count, saleInformations.Count());

                                        // 終了した場合はタイトルを終了済みにする
                                        UpdateArticle(saleInformation);
                                    }
                                    catch (Exception e)
                                    {
                                        logger?.LogError(e.Message);
                                    }
                                }
                                else
                                {
                                    // まだ投稿していない場合は商品情報を取得して投稿する
                                    try
                                    {
                                        logger?.LogInformation($"----- {saleInformation.NodeId} の商品情報取得開始 -----");
                                        if (ItemSearchAll(saleInformation) == true)
                                        {
                                            CheckSalePeriod(saleInformation, count, saleInformations.Count());

                                            PostToBlog(saleInformation);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        logger?.LogError(e.Message);
                                    }
                                }
                            }
                        }
                        else
                        {
                            logger?.LogInformation($"Skip {saleInformation.NodeId}");
                        }
                        count++;
                        logger?.LogInformation($"[{count}/{saleInformations.Count()}件完了]");

                        context.SaveChanges();

                        // デバッグ用に指定回数だけ実行する
                        //if ((options.Debug.NumberOfNodesToGet > 0) && (count >= options.Debug.NumberOfNodesToGet)) break;
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e.Message);
                throw;
            }

            // 開催中セール一覧のページを更新
            UpdateCurrentSaleListPage();

            // 最新セール一覧のページを更新
            UpdateLatestSaleListPage();

            logger?.LogInformation("----- End -----");
        }

        /// <summary>
        /// セールの一覧を取得
        /// </summary>
        /// <param name="nodeId">基準となるノードID</param>
        void BrowseNodeLookup(string browseNodeId)
        {
            IDictionary<string, string> request = new Dictionary<string, String>
            {
                ["Service"] = service,
                ["Version"] = apiVersion,
                ["Operation"] = "BrowseNodeLookup",
                ["SearchIndex"] = "KindleStore",
                ["BrowseNodeId"] = browseNodeId
            };

            using (var context = new KindleDbContext())
            {
                logger?.LogInformation($"現在の件数:{context.SaleInformations.Count()}件");
            }
            logger?.LogInformation($"セール情報一覧取得開始");

            // リクエストを送信して xml を取得
            var result = GetXml(request);

            // 取得した XML を読み込む
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

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
            logger?.LogInformation($"セール情報一覧取得完了({newSaleInformations.Count()}件)");

            // 現在のリストの項目が最新のリスト中になければ古い情報と判断して削除する
            int deleteCount = 0;

            using (var context = new KindleDbContext())
            {
                var saleInformations = context.SaleInformations.ToList();
                foreach (var saleInformation in saleInformations)
                {
                    var foundItem = newSaleInformations.Find(item => item.NodeId == saleInformation.NodeId);

                    if (foundItem == null)
                    {
                        context.SaleInformations.Remove(saleInformation);
                        deleteCount++;
                    }
                }
                context.SaveChanges();
                logger?.LogInformation($"{deleteCount}件の古いデータを削除(残り{context.SaleInformations.Count()}件)");
            }


            // 最新のリストのうち、現在のリスト中にないものだけ現在のリストに新規に追加する
            int addCount = 0;
            using (var context = new KindleDbContext())
            {
                foreach (var newSaleInformation in newSaleInformations)
                {
                    var foundItem = context.SaleInformations.Where(item => item.NodeId == newSaleInformation.NodeId).FirstOrDefault();

                    if (foundItem == null)
                    {
                        context.SaleInformations.Add(newSaleInformation);
                        addCount++;
                    }
                }
                context.SaveChanges();
                logger?.LogInformation($"{addCount}件の新規データを追加(計{context.SaleInformations.Count()}件)");
            }

            // ソート
            //data.SaleInformations.Sort((a, b) => string.Compare(a.NodeId, b.NodeId));
        }

        /// <summary>
        /// 全ページのアイテム情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemSearch.html
        bool ItemSearchAll(SaleInformation saleInformation)
        {
            int page = 0;
            bool result = false;

            do
            {
                Task.Delay(requestWaitTimerMSec).Wait();
                page++;
                result = ItemSearch(saleInformation, page);
                if (result == false)
                {
                    break;
                }

                // 残りページなし
                if ((saleInformation.TotalResults - page * 10) <= 0)
                {
                    break;
                }

                // AWS の仕様上、10ページまでしか取得できない
            } while (page < 10);

            return result;
        }
        /// <summary>
        /// 指定したページのアイテム情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemSearch.html
        bool ItemSearch(SaleInformation saleInformation, int page)
        {
            IDictionary<string, string> request = new Dictionary<string, String>
            {
                ["Service"] = service,
                ["Version"] = apiVersion,
                ["Operation"] = "ItemSearch",
                ["SearchIndex"] = "KindleStore",
                ["ResponseGroup"] = "Medium",
                ["BrowseNode"] = saleInformation.NodeId,
                ["ItemPage"] = page.ToString()
            };

            logger?.LogInformation($"商品情報取得開始({page}ページ目)");

            // リクエストを送信して xml を取得
            var result = GetXml(request);

            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(doc.NameTable);
            xmlNsManager.AddNamespace("ns", ns);

            // エラー情報の取得
            try
            {
                var error = doc.SelectSingleNode("ns:ItemSearchResponse/ns:Items/ns:Request/ns:Errors/ns:Error/ns:Code", xmlNsManager).InnerText;
                logger?.LogWarning("エラー情報あり：" + error);
                saleInformation.Error = true;
            }
            catch (Exception)
            {
                //Console.WriteLine("エラー情報なし：" + e.Message);
                saleInformation.Error = false;
            }

            try
            {
                var totalResults = doc.SelectSingleNode("ns:ItemSearchResponse/ns:Items/ns:TotalResults", xmlNsManager).InnerText;
                saleInformation.TotalResults = int.Parse(totalResults);
            }
            catch (Exception e)
            {
                logger?.LogError("TotalResults取得不可：" + e.Message);
            }

            // 商品情報を取得
            try
            {
                XmlNodeList nodeList = doc.SelectNodes("ns:ItemSearchResponse/ns:Items/ns:Item", xmlNsManager);

                foreach (XmlNode node in nodeList)
                {
                    try
                    {
                        saleInformation.Items.Add(new ItemDetail()
                        {
                            Title = node.SelectSingleNode("ns:ItemAttributes/ns:Title", xmlNsManager)?.InnerText,
                            PublicationDate = node.SelectSingleNode("ns:ItemAttributes/ns:PublicationDate", xmlNsManager)?.InnerText,
                            Content = node.SelectSingleNode("ns:EditorialReviews/ns:EditorialReview/ns:Content", xmlNsManager)?.InnerText,
                            Asin = node.SelectSingleNode("ns:ASIN", xmlNsManager)?.InnerText,
                            DetailPageUrl = node.SelectSingleNode("ns:DetailPageURL", xmlNsManager)?.InnerText,
                            MediumImageUrl = node.SelectSingleNode("ns:MediumImage/ns:URL", xmlNsManager)?.InnerText,
                            LargeImageUrl = node.SelectSingleNode("ns:LargeImage/ns:URL", xmlNsManager)?.InnerText
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogError("nodeListなし：" + e.Message);
            }

            logger?.LogInformation($"商品情報取得完了({page}ページ目、{saleInformation.Items.Count()}/{saleInformation.TotalResults}件)");

            return true;
        }

        /// <summary>
        /// 個別に情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemLookup.html
        void ItemLookUp(string asin)
        {
            IDictionary<string, string> request = new Dictionary<string, String>
            {
                ["Service"] = service,
                ["Version"] = apiVersion,
                ["Operation"] = "ItemLookup",
                ["IdType"] = "ASIN",
                ["ItemId"] = asin
            };

            logger?.LogInformation($"ASIN {asin} の詳細取得開始");

            // リクエストを送信して xml を取得
            var result = GetXml(request);

            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(doc.NameTable);
            xmlNsManager.AddNamespace("ns", ns);

            logger?.LogInformation($"ASIN {asin} の詳細取得完了");
        }

        /// <summary>
        /// ブログに投稿する
        /// </summary>
        void PostToBlog(SaleInformation saleInformation)
        {
            if (saleInformation.Error == true)
            {
                return;
            }

            try
            {
                var postInformation = blogger.InsertPost(CreateArticle(saleInformation));
                saleInformation.Url = postInformation.Url;
                saleInformation.PostId = postInformation.PostId;

                logger?.LogInformation($"投稿完了\n{saleInformation.Url}\n{saleInformation.PostId}");
            }
            catch (Exception e)
            {
                logger?.LogError("投稿失敗\n" + e.Message);
                throw;
            }
        }

        /// <summary>
        /// 投稿済みの記事を更新する
        /// </summary>
        void UpdateArticle(SaleInformation saleInformation)
        {
            try
            {
                var postInformation = blogger.UpdatePost(CreateArticle(saleInformation), new PostInformation() { Url = saleInformation.Url, PostId = saleInformation.PostId });
                saleInformation.Url = postInformation.Url;
                saleInformation.PostId = postInformation.PostId;

                logger?.LogInformation($"{saleInformation.Url}\n{saleInformation.PostId}");
            }
            catch (Exception e)
            {
                logger?.LogError("記事の更新失敗\n" + e.Message);
                throw;
            }
        }

        /// <summary>
        /// 記事の内容を作成する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// <returns></returns>
        Article CreateArticle(SaleInformation saleInformation)
        {
            var article = new Article() { title = saleInformation.Name };

            // 既に終了済みだったらタイトルに記載する
            if (saleInformation.SaleFinished == true)
            {
                article.title = "【終了】" + article.title;
            }

            // タイトルからラベルを抽出
            article.labels = ExtractLabels(article.title);

            article.content += $@"<p>
            対象は{saleInformation.TotalResults}冊。<br>
            Amazon のセールページは<a class='amazon' href='{GetAssociateLinkByBrowseNode(saleInformation.NodeId)}' target='_blank'>こちら</a>。
            </p>";

            article.content += @"<!--more-->";

            // Bootstrap のグリッドシステムを使って配置する
            // スマホでは２列で、タブレット以上では４列で表示する
            int colMax = 4;
            article.content += "\n<div class=\"container-fluid\">\n";
            int count = 0;
            foreach (var item in saleInformation.Items)
            {
                // ４列ごとに row で括る
                if (count % colMax == 0) article.content += "<div class=\"row flex\">\n";
                article.content += $@"
                <div class=""col-xs-6 col-sm-3 col-md-3 col-lg-3"">
                <a class='amazon' href='{item.DetailPageUrl}' target='_blank'><img src='{item.MediumImageUrl}' /></a><br>
                <a class='amazon' href='{item.DetailPageUrl}' target='_blank'>{item.Title}</a>
                </div>";
                article.content += "\n";
                if (count % colMax == colMax - 1) article.content += "</div>\n";
                count++;
            }
            // アイテム数が４の倍数でない場合、最後の row が閉じられていないのでここで閉じる
            if (count % colMax != 0) article.content += "</div>\n";
            article.content += "</div>\n";

            if (saleInformation.TotalResults > 100)
            {
                article.content += $@"<p><a class='amazon' href='{GetAssociateLinkByBrowseNode(saleInformation.NodeId)}' target='_blank'>もっと見る</a></p>";
            }

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
            pairs.Add("セール", new[] { "セール" });

            foreach (var key in pairs.Keys)
            {
                if (title.Contains(pairs[key]) == true)
                {
                    labels.Add(key);
                }
            }

            if (labels.Count == 0) labels.Add("その他");

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
        public (string startDate, string endDate) ExtractSalePeriod(SaleInformation saleInformation, Stream stream)
        {
            // 指定したサイトのHTMLをストリームで取得する

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
                        return (result.Groups["StartDate"].Value, result.Groups["EndDate"].Value);
                    }
                }
            }

            return (null, null);
        }

        /// <summary>
        /// 開催中セール一覧のページを更新する
        /// </summary>
        /// <returns></returns>
        void UpdateCurrentSaleListPage()
        {
            // 逆順で表示
            //var saleInformations = data.SaleInformations.Reverse<SaleInformation>();



            string content = $@"<p>
            Amazon Product Advertising API から取得した、現在開催中の Kindle セールの一覧です。<br>
            </p>
            <p>
            更新日時：{lastUpdate}
            </p>";

            int count = 0;
            content += @"<div class=""table-responsive"">
            <table class=""table"">
            <tr><th>No.</th><th>開催期間</th><th>タイトル</th><th>Amazon</th></tr>";

            using (var context = new KindleDbContext())
            {
                foreach (var saleInformation in context.SaleInformations.OrderByDescending(x => x.NodeId))
                {
                    if ((saleInformation.Error == false) && (saleInformation.SaleStarted == true) && (saleInformation.SaleFinished == false))
                    {
                        string entry;
                        if (saleInformation.Url == null)
                        {
                            entry = saleInformation.Name;
                        }
                        else
                        {
                            entry = $"<a href='{saleInformation.Url}'>{saleInformation.Name}</a>";
                        }

                        content += $@"<tr>
                    <td>{count++}</td>
                    <td>{saleInformation.GetSalePeriod()}</td>
                    <td>{entry}</td>
                    <td><a class='amazon' href='{GetAssociateLinkByBrowseNode(saleInformation.NodeId)}' target='_blank'>Amazon</a></td>
                    </tr>";
                    }
                }
            }
            content += "</table></div>";

            try
            {
                blogger.UpdatePage(options.CurrentSaleListPageId, content);
            }
            catch
            {
                logger?.LogError("開催中セール一覧のページ更新失敗");
                throw;
            }
        }

        /// <summary>
        /// 最新セール情報一覧のページを更新する
        /// </summary>
        /// <returns></returns>
        void UpdateLatestSaleListPage()
        {
            // 逆順で表示
            //var saleInformations = data.SaleInformations.Reverse<SaleInformation>();

            string content = $@"<p>
            Amazon Product Advertising API から取得した Kindle のセールページ一覧です。<br>
            この中にはまだセールを開始していないものや、既に終了したセールなど、セールではないものもありますのでご注意ください。
            </p>
            <p>
            更新日時：{lastUpdate}
            </p>";

            int count = 0;
            content += @"<div class=""table-responsive"">
            <table class=""table"">
            <tr><th>No.</th><th>開催期間</th><th>タイトル</th><th>Amazon</th><th>ESF</th></tr>";

            using (var context = new KindleDbContext())
            {
                foreach (var saleInformation in context.SaleInformations.OrderByDescending(x => x.NodeId))
                {
                    string entry;
                    if (saleInformation.Url == null)
                    {
                        entry = saleInformation.Name;
                    }
                    else
                    {
                        entry = $"<a href='{saleInformation.Url}'>{saleInformation.Name}</a>";
                    }

                    content += $@"<tr>
                <td>{count++}</td>
                <td>{saleInformation.GetSalePeriod()}</td>
                <td>{entry}</td>
                <td><a class='amazon' href='{GetAssociateLinkByBrowseNode(saleInformation.NodeId)}' target='_blank'>Amazon</a></td>
                <td>{(saleInformation.Error ? "1" : "0")}{(saleInformation.SaleStarted ? "1" : "0")}{(saleInformation.SaleFinished ? "1" : "0")}</td>
                </tr>";
                }
            }
            content += "</table></div>";

            try
            {
                blogger.UpdatePage(options.LatestSaleListPageId, content);
            }
            catch
            {
                logger?.LogError("最新セール情報一覧のページ更新失敗");
                throw;
            }
        }

        /// <summary>
        /// セール期間を判別してセットする
        /// </summary>
        /// <param name="saleInformation"></param>
        /// <param name="count">現在何件目のセール情報</param>
        /// <param name="total">セール情報のトータル</param>
        void CheckSalePeriod(SaleInformation saleInformation, int count, int total)
        {
            if (saleInformation.Error == true) return;

            // タイトルから終了日を判別
            saleInformation.SetSalePeriod(DateTime.Now, null, ExtractEndDate(saleInformation.Name), count, total);
            if (saleInformation.EndDate != null)
            {
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
            using (var stream = client.GetStreamAsync(GetAssociateLinkByBrowseNode(saleInformation.NodeId)).Result)
            {
                (var startDate, var endDate) = ExtractSalePeriod(saleInformation, stream);
                saleInformation.SetSalePeriod(DateTime.Now, startDate, endDate, count, total);
            }

            if (saleInformation.StartDate != null && saleInformation.EndDate != null)
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

using System;
using System.Collections.Generic;
using System.Text;

using System.Threading.Tasks;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

using AngleSharp.Parser.Html;

using MT1.Options;
using MT1.GoogleApi;
using MT1.Extensions;


namespace MT1.AmazonProductAdvtApi.HRHM
{
    public class HRHM : Amazon
    {
        Blogger blogger;

        HRHMData data;

        ILogger logger;
        HRHMOptions options;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public HRHM(ILogger<HRHM> logger, IOptions<HRHMOptions> hrHMoptions, IOptions<AmazonOptions> amazonOptions) : base(logger, amazonOptions)
        {
            this.logger = logger;
            options = hrHMoptions?.Value;

            if (options != null)
            {
                blogger = new Blogger(logger, options.BlogId);
            }

            var serializer = new XmlSerializer(typeof(HRHMData));
            try
            {
                using (var fs = new FileStream(options.GetDataFilePath(), FileMode.Open))
                {
                    data = (HRHMData)serializer.Deserialize(fs);
                }
                logger.LogInformation("デシリアライズ完了");
            }
            catch
            {
                logger.LogError("デシリアライズ失敗");
                data = new HRHMData();
            }
        }

        /// <summary>
        /// 処理の開始
        /// </summary>
        /// <param name="args"></param>
        public async void Run()
        {
            logger.LogInformation("----- HRHM Begin -----");

            try
            {
                // セールの一覧を取得
                await ItemSearchAsync("569298");
            }
            catch (Exception e)
            {
                logger.LogError("一覧取得失敗" + e.Message);
            }

            logger.LogInformation("----- HRHM End -----");
        }

        /// <summary>
        /// 指定したページのアイテム情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemSearch.html
        async Task<bool> ItemSearchAsync(string nodeId)
        {
            IDictionary<string, string> request = new Dictionary<string, String>
            {
                ["Service"] = service,
                ["Version"] = apiVersion,
                ["Operation"] = "ItemSearch",
                ["SearchIndex"] = "Music",
                ["ResponseGroup"] = "Medium",
                ["BrowseNode"] = nodeId
            };

            logger.LogInformation($"CD 情報一覧取得開始");

            // リクエストを送信して xml を取得
            var result = await GetXmlAsync(request);

            // 取得した XML を読み込む
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            WriteXml(doc, options.GetNodeListFilePath());

            return true;
        }
    }
}

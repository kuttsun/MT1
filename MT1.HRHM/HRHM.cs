using System;
using System.Collections.Generic;
using System.Text;

using System.Threading.Tasks;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

using MT1.Core.Amazon;
using MT1.Core.Google.Blogger;
using MT1.Core.Extensions;


namespace MT1.HRHM
{
    public class HRHM : Amazon
    {
        IBlogger blogger;

        HRHMData data;

        ILogger logger;
        HRHMOptions options;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public HRHM(IBlogger blogger, ILogger<HRHM> logger, IOptions<HRHMOptions> hrHMoptions, IOptions<AmazonOptions> amazonOptions) : base(logger, amazonOptions)
        {
            this.logger = logger;
            options = hrHMoptions?.Value;
            this.blogger = blogger;

            this.blogger.BlogId = options.BlogId;

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
        public void Run()
        {
            logger.LogInformation("----- HRHM Begin -----");

            try
            {
                // セールの一覧を取得
                ItemSearch("569298");

                //SerializeData(options.GetDataFilePath());
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                throw;
            }

            logger.LogInformation("----- HRHM End -----");
        }

        void SerializeData(string filePath)
        {
            var xmlSerializer = new XmlSerializer(typeof(HRHMData));
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                xmlSerializer.Serialize(fileStream, data);
            }
        }

        /// <summary>
        /// 指定したページのアイテム情報を取得する
        /// </summary>
        /// <param name="saleInformation"></param>
        /// https://images-na.ssl-images-amazon.com/images/G/09/associates/paapi/dg/index.html?ItemSearch.html
        bool ItemSearch(string nodeId)
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
            var result = GetXml(request);

            // 取得した XML を読み込む
            XmlDocument doc = new XmlDocument();
            doc.Load(result);

            WriteXml(doc, options.GetNodeListFilePath());

            return true;
        }
    }
}

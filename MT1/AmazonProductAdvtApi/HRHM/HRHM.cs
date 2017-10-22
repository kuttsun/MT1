using System;
using System.Collections.Generic;
using System.Text;

using System.Threading.Tasks;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

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
        }
    }
}

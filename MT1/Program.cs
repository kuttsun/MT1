using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

using MT1.Options;
using MT1.AmazonProductAdvtApi.Kindle;
using MT1.AmazonProductAdvtApi.HRHM;


namespace MT1
{
    class Program
    {
        static void Main(string[] args)
        {
            // 文字化け対策 http://kagasu.hatenablog.com/entry/2016/12/07/004813
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IServiceCollection serviceCollection = new ServiceCollection();

            // ConfigureServices で DI の準備を行う
            ConfigureServices(serviceCollection);

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            //serviceProvider.GetService<Kindle>().Run();
            serviceProvider.GetService<HRHM>().Run();

            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // ロギングの設定
            ILoggerFactory loggerFactory = new LoggerFactory()
                // コンソールに出力する
                .AddConsole()
                // Visual Studio のデバッグウィンドウに出力する
                .AddDebug();

            // DI サービスコンテナに Singleton ライフサイクルにてオブジェクトを登録する
            // Singleton ライフサイクルでは Dependency インスタンスを一つ生成し、そのインスタンスをアプリケーションで共有する
            services.AddSingleton(loggerFactory);
            // AddLogging メソッドを呼び出すことで ILoggerFactory と ILogger<T> が DI 経由で扱えるようになる
            services.AddLogging();

            // IConfigurationBuilder で設定を選択
            // IConfigurationBuilder.Build() で設定情報を確定し、IConfigurationRoot を生成する
            IConfigurationRoot configuration = new ConfigurationBuilder()
                // 基準となるパスを設定
                .SetBasePath(Directory.GetCurrentDirectory())
                // ここでどの設定元を使うか指定
                // 同じキーが設定されている場合、後にAddしたものが優先される
                .AddJsonFile("appsettings.json", optional: false)
                // ここでは JSON より環境変数を優先している
                //.AddEnvironmentVariables()
                // 上記の設定を実際に適用して構成読み込み用のオブジェクトを得る
                .Build();

            // Logger と同じく DI サービスコンテナに Singleton ライフサイクルにてオブジェクトを登録する
            services.AddSingleton(configuration);

            // オプションパターンを有効にすることで、構成ファイルに記述した階層構造データを POCO オブジェクトに読み込めるようにする
            services.AddOptions();

            // Configure<T> を使ってオプションを初期化する
            // IConfigurationRoot から GetSection 及び GetChildren で個々の設定の取り出しができる
            // ここでは "MyOptions" セクションの内容を MyOptions として登録
            services.Configure<AmazonOptions>(configuration.GetSection(nameof(AmazonOptions)));
            services.Configure<KindleOptions>(configuration.GetSection(nameof(KindleOptions)));
            services.Configure<HRHMOptions>(configuration.GetSection(nameof(HRHMOptions)));

            // Application を DI サービスコンテナに登録する
            // AddTransient はインジェクション毎にインスタンスが生成される
            services.AddTransient<Kindle>();
            services.AddTransient<HRHM>();
        }
    }
}
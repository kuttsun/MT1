using System;
using System.Text;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.CommandLineUtils;

using MT1.Core.Amazon;
using MT1.Core.Google.Blogger;

namespace MT1.HRHM
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

            // コマンドライン引数に対する処理を定義
            var cla = CommandLine(serviceProvider);

            // コマンドライン引数に応じた処理を実行
            cla.Execute(args);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

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
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
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
            services.Configure<HRHMOptions>(configuration.GetSection(nameof(HRHMOptions)));

            // IBlogger を DI サービスコンテナに登録
            services.AddTransient<IBlogger, Blogger>();

            // Application を DI サービスコンテナに登録する
            // AddTransient はインジェクション毎にインスタンスが生成される
            services.AddTransient<HRHM>();
        }

        static CommandLineApplication CommandLine(IServiceProvider serviceProvider)
        {
            Assembly.GetExecutingAssembly();

            // プログラム引数の解析
            var cla = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                // アプリケーション名（ヘルプの出力で使用される）
                Name = "MT1.HRHM",
            };

            // ヘルプ出力のトリガーとなるオプションを指定
            cla.HelpOption("-?|-h|--help");

            // デバッグ・メンテナンス操作
            cla.Command("debug", command =>
            {
                // 説明（ヘルプの出力で使用される）
                command.Description = "Debug Mode";

                command.HelpOption("-?|-h|--help");

                command.OnExecute(() =>
                {
                    //serviceProvider.GetService<Kindle>().Run();
                    return 0;
                });
            });

            // デフォルトの動作
            cla.OnExecute(() =>
            {
                try
                {
                    serviceProvider.GetService<HRHM>().Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine("異常終了" + e.Message);
                }
                return 0;
            });

            return cla;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Extensions.Configuration;
// Microsoft.Extensions.Configuration.Json と Microsoft.Extensions.Configuration.EnvironmentVariables のインストールも必要

namespace MT1.AmazonProductAdvtApi.Kindle
{
    // http://kikki.hatenablog.com/entry/2016/11/30/000000
    class Configuration
    {

        public static IConfigurationRoot Config { get; private set; }

        public Configuration()
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"appsettings.json", true)
                .AddEnvironmentVariables()
                .Build();

            Console.Write($"Hello, {Config["appSettings:frameworkName"]}"); // "Hello, .NET Core"
        }
    }
}

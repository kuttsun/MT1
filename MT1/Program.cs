﻿using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MT1.AmazonProductAdvtApi.Kindle;

namespace MT1
{
    class Program
    {
        static void Main(string[] args)
        {
            // 文字化け対策 http://kagasu.hatenablog.com/entry/2016/12/07/004813
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Console.WriteLine("----- Program Start -----");

            Kindle kindle;
            var serializer = new XmlSerializer(typeof(Kindle));
            try
            {
                using (var fs = new FileStream("KindleSaleInformations.xml", FileMode.Open))
                {
                    kindle = (Kindle)serializer.Deserialize(fs);
                }
            }
            catch
            {
                kindle = new Kindle();
            }
            kindle.GetSaleInformations(null);

            Console.ReadKey();
        }
    }
}
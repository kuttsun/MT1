using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MT1.AmazonProductAdvtApi;

namespace MT1
{
    class Program
    {
        static void Main(string[] args)
        {
            // 文字化け対策 http://kagasu.hatenablog.com/entry/2016/12/07/004813
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Console.WriteLine("----- Begin -----");

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

            Console.WriteLine("Kindle 情報取得完了");

            Console.WriteLine("----- End -----");
            Console.ReadKey();
        }
    }
}
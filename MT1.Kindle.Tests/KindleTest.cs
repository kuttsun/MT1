using System;

using Microsoft.Extensions.Logging;

using Xunit;

using MT1.Kindle;

namespace MT1.Kindle.Tests
{
    public class KindleTest
    {
        ILoggerFactory loggerFactory;
        ILogger<Kindle> logger;

        public KindleTest()
        {
            loggerFactory = new LoggerFactory();
            logger = loggerFactory.CreateLogger<Kindle>();
        }

        [Theory,
            InlineData("割引", "【期間限定無料&amp;50%OFF】「夏☆電書」少女・女性コミックセール （8/24まで）"),
            InlineData("ポイント還元", "【50 % ポイント還元】エンタメから新本格まで講談社のミステリーフェア"),
            InlineData("キャンペーン", "【50%ポイント還元】　SBクリエイティブキャンペーン"),
            InlineData("期間限定", "【期間限定無料お試し&amp;50%OFF】夏のTLまつり第1弾 山口ねね新刊発売記念フェア （8/10まで）"),
            InlineData("無料", "【期間限定無料お試し&amp;50%OFF】夏のTLまつり第1弾 山口ねね新刊発売記念フェア （8/10まで）"),
            InlineData("均一", "【200円均一】青年マンガ　1～5巻セール（8/3まで）"),
            InlineData("特集・フェア", "【50%OFF】2017夏のビジネス・実用書フェア （7/27まで）"),
            InlineData("セール", "Kindle本セール"),
            InlineData("その他", "おすすめの本 ")]
        public void ExtractTagsTest(string expected, string title)
        {
            var kindle = new Kindle(null, logger, null, null);
            Assert.True(kindle.ExtractLabels(title).Contains(expected));
        }

        [Theory,
            InlineData("8/17", "【期間限定無料&amp;50%OFF】「夏☆電書」女の恨み～リベンジ～特集 (8/17まで)"),
            InlineData("6/8", "【期間限定無料お試し&amp;通常版半額】小説・ラノベのコミカライズ作品フェア（6/8まで）"),
            InlineData("8/14", "【最大50%ポイント還元】3日間限定全点フェア（8/14まで）（終了"),
            InlineData("11/16", "【期間限定無料】過去・現在・未来…時代（とき）を翔けるマンガ特集(11/16まで)"),
            InlineData(null, "【50%ポイント還元】　SBクリエイティブキャンペーン")]
        public void ExtractEndDateTest(string expected, string title)
        {
            var kindle = new Kindle(null, logger, null, null);
            Assert.Equal(expected, kindle.ExtractEndDate(title));
        }
    }
}

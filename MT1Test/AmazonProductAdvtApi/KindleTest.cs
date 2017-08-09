using System;
using Xunit;

using MT1.AmazonProductAdvtApi;

namespace MT1.Test
{
    public class KindleTest
    {
        [Theory,
            InlineData("割引", "【期間限定無料&amp;50%OFF】「夏☆電書」少女・女性コミックセール （8/24まで）"),
            InlineData("ポイント還元", "【50 % ポイント還元】エンタメから新本格まで講談社のミステリーフェア"),
            InlineData("キャンペーン", "【50%ポイント還元】　SBクリエイティブキャンペーン"),
            InlineData("期間限定", "【期間限定無料お試し&amp;50%OFF】夏のTLまつり第1弾 山口ねね新刊発売記念フェア （8/10まで）"),
            InlineData("無料", "【期間限定無料お試し&amp;50%OFF】夏のTLまつり第1弾 山口ねね新刊発売記念フェア （8/10まで）"),
            InlineData("均一", "【200円均一】青年マンガ　1～5巻セール（8/3まで）"),
            InlineData("特集・フェア", "【50%OFF】2017夏のビジネス・実用書フェア （7/27まで）")]
        public void ExtractTagsTest(string expected, string title)
        {
            var kindle = new Kindle();
            Assert.True(kindle.ExtractLabels(title).Contains(expected));
        }
    }
}

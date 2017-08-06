using System;
using Xunit;

using MT1.AmazonProductAdvtApi;

namespace MT1.Test
{
    public class KindleTest
    {
        [Theory,
            InlineData("Š„ˆø","50%OFF")]
        public void ExtractTagsTest(string expected,string title)
        {
            var kindle = new Kindle();
            var tags = kindle.ExtractTags(title);
            Assert.True(tags.Contains(expected) );
        }
    }
}

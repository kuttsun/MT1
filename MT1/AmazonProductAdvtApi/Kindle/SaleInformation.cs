using System;
using System.Collections.Generic;
using System.Text;

using MT1.GoogleApi;

namespace MT1.AmazonProductAdvtApi.Kindle
{
    partial class Kindle
    {
        public class SaleInformation
        {
            public string NodeId = null;
            public string Name = null;
            public bool Error = false;
            public string MoreSearchResultsUrl = null;
            public List<ItemDetail> Items = null;
            public PostInformation PostInformation = null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MT1.HRHM.Options
{
    public class HRHMOptions
    {
        public string BlogId { get; set; }
        public string OutDir { get; set; }
        public string NodeListFile { get; set; }
        public string DataFile { get; set; }
        //public Debug Debug { get; set; }

        public string GetNodeListFilePath()
        {
            return OutDir + NodeListFile;
        }

        public string GetDataFilePath()
        {
            return OutDir + DataFile;
        }
    }
}

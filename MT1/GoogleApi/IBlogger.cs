using System;
using System.Collections.Generic;
using System.Text;

namespace MT1.GoogleApi
{
    interface IBlogger
    {
        PostInformation InsertPost(Article article);
        PostInformation UpdatePost(Article article, PostInformation postInformation);
        void UpdatePage(string pageId, string content);
    }
}

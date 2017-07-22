using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Blogger;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;

namespace MT1.GoogleApi
{
    class Blogger
    {
        string blogID;

        public Blogger(string blogID)
        {
            this.blogID = blogID;
        }

        public async void Post(string title, string content, IList<string> labels)
        {
            try
            {
                // OAuth 認証を行う
                UserCredential credential;
                using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                            GoogleClientSecrets.Load(stream).Secrets,
                            new[] { BloggerService.Scope.Blogger },
                            "user",
                            CancellationToken.None);
                }

                // Bloggerのインスタンスを取得
                var service = new BloggerService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Blogger Post"
                });


                // Blogの一覧を取得
                //var blogList = service.Blogs.ListByUser("self").Execute();

                // Blogに新しいエントリを作成する
                var newPost = new Post();
                newPost.Title = title;
                newPost.Content = content;
                newPost.Published = DateTime.Now;
                newPost.Labels = labels;
                var updPost = service.Posts.Insert(newPost, blogID).Execute();
                //...
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
        }
    }
}

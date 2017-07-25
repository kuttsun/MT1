using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Blogger;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;

namespace MT1.GoogleApi
{
    public class PostInformation
    {
        public string Url;
        public string PostId;
    }

    class Blogger
    {
        string blogID;
        UserCredential credential = null;
        BloggerService service = null;

        public Blogger(string blogID)
        {
            this.blogID = blogID;
        }

        async Task<UserCredential> GetCredentialAsync()
        {
            if (credential == null)
            {
                using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                            GoogleClientSecrets.Load(stream).Secrets,
                            new[] { BloggerService.Scope.Blogger },
                            "user",
                            CancellationToken.None);
                }
            }
            return credential;
        }

        async Task<BloggerService> GetServiceAsync()
        {
            // Bloggerのインスタンスを取得
            if (service == null)
            {
                var credential = await GetCredentialAsync();
                service = new BloggerService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Blogger Post"
                });
            }
            return service;
        }

        public async Task<PostInformation> PostAsync(string title, string content, IList<string> labels)
        {
            try
            {
                // Bloggerのインスタンスを取得
                var service = await GetServiceAsync();

                // Blogの一覧を取得
                //var blogList = service.Blogs.ListByUser("self").Execute();

                // Blogに新しいエントリを作成する
                var newPost = new Post();
                newPost.Title = title;
                newPost.Content = content;
                newPost.Published = DateTime.Now;
                newPost.Labels = labels;
                var updPost = service.Posts.Insert(newPost, blogID).Execute();

                return new PostInformation { Url = updPost.Url, PostId = updPost.Id };
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }

            return null;
        }
    }
}

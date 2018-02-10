using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;

using Microsoft.Extensions.Logging;

namespace MT1.Core.Google.Blogger
{
    public class Blogger : IBlogger
    {
        // エラー時の待機時間（100秒）
        readonly int requestLimitationMSec = 100 * 1000;
        public string BlogId { get; set; }
        UserCredential credential = null;
        BloggerService service = null;

        ILogger logger;

        public Blogger(ILogger<Blogger> logger)
        {
            this.logger = logger;
        }

        UserCredential GetCredential()
        {
            if (credential == null)
            {
                using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                            GoogleClientSecrets.Load(stream).Secrets,
                            new[] { BloggerService.Scope.Blogger },
                            "user",
                            CancellationToken.None).Result;
                }
            }
            return credential;
        }

        BloggerService GetService()
        {
            // Bloggerのインスタンスを取得
            if (service == null)
            {
                var credential = GetCredential();
                service = new BloggerService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Blogger Post"
                });
            }
            return service;
        }

        public PostInformation InsertPost(Article article)
        {
            // Bloggerのインスタンスを取得
            var service = GetService();

            // Blogに新しいエントリを作成する
            var newPost = new Post
            {
                Title = article.title,
                Content = article.content,
                Published = DateTime.Now
            };
            if (article.labels.Count > 0)
            {
                newPost.Labels = article.labels;
            }

            while (true)
            {
                try
                {
                    var updPost = service.Posts.Insert(newPost, BlogId).Execute();

                    return new PostInformation { Url = updPost.Url, PostId = updPost.Id, Published = updPost.Published };
                }
                catch (Exception e)
                {
                    logger.LogError("新規投稿失敗、一定時間後リトライします\n" + e.Message);
                    Task.Delay(requestLimitationMSec).Wait();
                }
            }
        }

        Post GetPost(string postId)
        {
            // Bloggerのインスタンスを取得
            var service = GetService();

            while (true)
            {
                try
                {
                    return service.Posts.Get(BlogId, postId).Execute();
                }
                catch (Exception e)
                {
                    logger.LogError("投稿取得失敗、一定時間後リトライします\n" + e.Message);
                    Task.Delay(requestLimitationMSec).Wait();
                }
            }
        }

        public PostInformation UpdatePost(Article article, PostInformation postInformation)
        {
            // Bloggerのインスタンスを取得
            var service = GetService();

            var newPost = new Post
            {
                Title = article.title,
                Content = article.content,
                // 投稿日時は変更しない
                Published = postInformation.Published
            };
            if (article.labels.Count > 0)
            {
                newPost.Labels = article.labels;
            }

            while (true)
            {
                try
                {
                    var updPost = service.Posts.Update(newPost, BlogId, postInformation.PostId).Execute();

                    // 更新後の情報を取得
                    return new PostInformation { Url = updPost.Url, PostId = updPost.Id, Published = updPost.Published };
                }
                catch (Exception e)
                {
                    logger.LogError($"BlogId:{BlogId}, PostId:{postInformation.PostId}\n投稿更新失敗、一定時間後リトライします\n" + e.Message);
                    if(e.Message.Contains("Message[Not Found] Location[ - ] Reason[notFound] Domain[global]"))
                    {
                        logger.LogWarning("記事が見つからないため Skip したほうがよい？");
                    }
                    Task.Delay(requestLimitationMSec).Wait();
                }
            }
        }

        public void UpdatePage(string pageId, string content)
        {
            // Bloggerのインスタンスを取得
            var service = GetService();

            while (true)
            {
                try
                {
                    // 現在のページを取得して更新する
                    var newPage = service.Pages.Get(BlogId, pageId).Execute();
                    newPage.Content = content;
                    var updPage = service.Pages.Update(newPage, BlogId, pageId).Execute();
                    return;
                }
                catch (Exception e)
                {
                    logger.LogError("ページ更新失敗、一定時間後リトライします\n" + e.Message);
                    Task.Delay(requestLimitationMSec).Wait();
                }
            }
        }
    }
}

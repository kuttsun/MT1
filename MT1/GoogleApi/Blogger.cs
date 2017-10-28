﻿using System;
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

namespace MT1.GoogleApi
{
    public class PostInformation
    {
        public string Url;
        public string PostId;
        public DateTime? Published;
    }

    public class Article
    {
        public string title;
        public string content;
        public IList<string> labels;
    }

    class Blogger
    {
        // エラー時の待機時間（100秒）
        readonly int requestLimitationMSec = 100 * 1000;
        string blogId;
        UserCredential credential = null;
        BloggerService service = null;

        ILogger logger;

        public Blogger(ILogger logger, string blogId)
        {
            this.logger = logger;
            this.blogId = blogId;
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

        public async Task<PostInformation> PostAsync(Article article)
        {
            // Bloggerのインスタンスを取得
            var service = await GetServiceAsync();

            // Blogに新しいエントリを作成する
            var newPost = new Post();
            newPost.Title = article.title;
            newPost.Content = article.content;
            newPost.Published = DateTime.Now;
            if (article.labels.Count > 0)
            {
                newPost.Labels = article.labels;
            }

            while (true)
            {
                try
                {
                    var updPost = service.Posts.Insert(newPost, blogId).Execute();

                    return new PostInformation { Url = updPost.Url, PostId = updPost.Id, Published = updPost.Published };
                }
                catch (Exception e)
                {
                    logger.LogError("新規投稿失敗、一定時間後リトライします\n" + e.Message);
                    await Task.Delay(requestLimitationMSec);
                }
            }
        }

        async Task<Post> GetPostAsync(string postId)
        {
            // Bloggerのインスタンスを取得
            var service = await GetServiceAsync();

            while (true)
            {
                try
                {
                    return service.Posts.Get(blogId, postId).Execute();
                }
                catch (Exception e)
                {
                    logger.LogError("投稿取得失敗、一定時間後リトライします\n" + e.Message);
                    await Task.Delay(requestLimitationMSec);
                }
            }
        }

        public async Task<PostInformation> UpdatePostAsync(Article article, PostInformation postInformation)
        {
            // Bloggerのインスタンスを取得
            var service = await GetServiceAsync();

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
                    var updPost = service.Posts.Update(newPost, blogId, postInformation.PostId).Execute();

                    // 更新後の情報を取得
                    return new PostInformation { Url = updPost.Url, PostId = updPost.Id, Published = updPost.Published };
                }
                catch (Exception e)
                {
                    logger.LogError("投稿更新失敗、一定時間後リトライします\n" + e.Message);
                    await Task.Delay(requestLimitationMSec);
                }
            }
        }

        /// <summary>
        /// ページを更新する
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task UpdatePageAsync(string pageId, string content)
        {
            // Bloggerのインスタンスを取得
            var service = await GetServiceAsync();

            while (true)
                try
                {
                    // 現在のページを取得して更新する
                    var newPage = service.Pages.Get(blogId, pageId).Execute();
                    newPage.Content = content;
                    var updPage = service.Pages.Update(newPage, blogId, pageId).Execute();
                }
                catch (Exception e)
                {
                    logger.LogError("ページ更新失敗、一定時間後リトライします\n" + e.Message);
                    await Task.Delay(requestLimitationMSec);
                }
        }
    }
}

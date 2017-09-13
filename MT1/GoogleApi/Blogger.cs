﻿using System;
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

using Microsoft.Extensions.Logging;

namespace MT1.GoogleApi
{
    public class PostInformation
    {
        public string Url;
        public string PostId;
    }

    public class Article
    {
        public string title;
        public string content;
        public IList<string> labels;
    }

    class Blogger
    {
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
            try
            {
                // Bloggerのインスタンスを取得
                var service = await GetServiceAsync();

                // Blogの一覧を取得
                //var blogList = service.Blogs.ListByUser("self").Execute();

                // Blogに新しいエントリを作成する
                var newPost = new Post();
                newPost.Title = article.title;
                newPost.Content = article.content;
                newPost.Published = DateTime.Now;
                if (article.labels.Count > 0)
                {
                    newPost.Labels = article.labels;
                }
                var updPost = service.Posts.Insert(newPost, blogId).Execute();

                return new PostInformation { Url = updPost.Url, PostId = updPost.Id };
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }

            return null;
        }

        public async Task UpdatePostAsync(Article article, PostInformation postInformation)
        {

            // Bloggerのインスタンスを取得
            var service = await GetServiceAsync();

            // 現在のエントリを取得して更新する
            Post newPost;
            try
            {
                newPost = service.Posts.Get(blogId, postInformation.PostId).Execute();
            }
            catch (Exception)
            {
                throw;
            }
            newPost.Title = article.title;
            newPost.Content = article.content;
            if (article.labels.Count > 0)
            {
                newPost.Labels = article.labels;
            }

            // API がエラーを返した場合は指数バックオフによりリトライする
            int timer = 1000;
            while (true)
            {
                try
                {
                    var updPost = service.Posts.Update(newPost, blogId, postInformation.PostId).Execute();

                    // 更新後の情報を取得
                    postInformation.Url = updPost.Url;
                    postInformation.PostId = updPost.Id;
                    break;
                }
                catch (Exception e)
                {
                    logger.LogError("投稿失敗、リトライします\n" + e.Message);
                    await Task.Delay(timer);
                    timer *= 2;
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
            try
            {
                // Bloggerのインスタンスを取得
                var service = await GetServiceAsync();

                // 現在のページを取得して更新する
                var newPage = service.Pages.Get(blogId, pageId).Execute();
                newPage.Content = content;
                var updPage = service.Pages.Update(newPage, blogId, pageId).Execute();
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
        }
    }
}

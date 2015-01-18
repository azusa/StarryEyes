﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncOAuth;
using JetBrains.Annotations;
using StarryEyes.Anomaly.Ext;
using StarryEyes.Anomaly.TwitterApi;
using StarryEyes.Anomaly.TwitterApi.Rest.Infrastructure;
using StarryEyes.Anomaly.TwitterApi.Rest.Parameter;
using StarryEyes.Anomaly.Utils;

namespace StarryEyes.Anomaly
{
    public static class OAuthCredentialExtension
    {
        private const string UserAgentHeader = "User-Agent";

        private const string ParameterAllowedChars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";


        public static HttpClient CreateOAuthClient(
            this IOAuthCredential credential,
            IEnumerable<KeyValuePair<string, string>> optionalHeaders = null,
            bool useGZip = true)
        {
            return new HttpClient(
                new OAuthMessageHandler(
                    GetInnerHandler(useGZip),
                    credential.OAuthConsumerKey, credential.OAuthConsumerSecret,
                    new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                    optionalHeaders), true)
                .SetUserAgent(ApiAccessProperties.UserAgent);
        }

        public static HttpClient CreateOAuthEchoClient(
            this IOAuthCredential credential,
            string serviceProvider, string realm,
            IEnumerable<KeyValuePair<string, string>> optionalHeaders = null,
            bool useGZip = true)
        {
            return new HttpClient(
                new OAuthEchoMessageHandler(
                    GetInnerHandler(useGZip),
                    serviceProvider, realm, credential.OAuthConsumerKey, credential.OAuthConsumerSecret,
                    new AccessToken(credential.OAuthAccessToken, credential.OAuthAccessTokenSecret),
                    optionalHeaders), true)
                .SetUserAgent(ApiAccessProperties.UserAgent);
        }

        public static Task<HttpResponseMessage> PostAsync(
            this IOAuthCredential credential,
            string path, Dictionary<string, object> param,
            CancellationToken cancellationToken)
        {
            return credential.PostAsync(path, param.ParametalizeForPost(), cancellationToken);
        }

        public static async Task<HttpResponseMessage> PostAsync(
            this IOAuthCredential credential,
            string path, HttpContent content,
            CancellationToken cancellationToken)
        {
            var client = credential.CreateOAuthClient();
            return await client.PostAsync(FormatUrl(path), content, cancellationToken);
        }

        public static async Task<HttpResponseMessage> GetAsync(
            this IOAuthCredential credential,
            string path, Dictionary<string, object> param,
            CancellationToken cancellationToken)
        {
            var client = credential.CreateOAuthClient();
            return await client.GetAsync(FormatUrl(path, param.ParametalizeForGet()),
                cancellationToken);
        }

        public static async Task<string> GetStringAsync(
            this IOAuthCredential credential,
            string path, Dictionary<string, object> param,
            CancellationToken cancellationToken)
        {
            var client = credential.CreateOAuthClient();
            var resp = await client.GetAsync(FormatUrl(path, param.ParametalizeForGet()),
                 cancellationToken);
            return await resp.ReadAsStringAsync();
        }


        private static string FormatUrl(string path)
        {
            return HttpUtility.ConcatUrl(ApiAccessProperties.ApiEndpoint, path);
        }

        private static string FormatUrl(string path, string param)
        {
            return String.IsNullOrEmpty(param)
                ? FormatUrl(path)
                : FormatUrl(path) + "?" + param;
        }

        public static HttpClient SetUserAgent(this HttpClient client, string userAgent)
        {
            // remove before add user agent
            client.DefaultRequestHeaders.Remove(UserAgentHeader);
            client.DefaultRequestHeaders.Add(UserAgentHeader, userAgent);
            return client;
        }

        private static HttpMessageHandler GetInnerHandler(bool useGZip)
        {
            var proxy = Core.GetWebProxy();
            return new TwitterApiExceptionHandler(
                new HttpClientHandler
                {
                    AutomaticDecompression = useGZip
                        ? DecompressionMethods.GZip | DecompressionMethods.Deflate
                        : DecompressionMethods.None,
                    Proxy = proxy,
                    UseProxy = proxy != null
                });
        }

        internal static FormUrlEncodedContent ParametalizeForPost(
            [NotNull] this Dictionary<string, object> dict)
        {
            return new FormUrlEncodedContent(
                dict.Where(kvp => kvp.Value != null)
                    .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString())));
        }

        internal static string ParametalizeForGet(
            [NotNull] this Dictionary<string, object> dict)
        {
            return dict.Where(kvp => kvp.Value != null)
                       .OrderBy(kvp => kvp.Key)
                       .Select(kvp => string.Format("{0}={1}",
                           kvp.Key, EncodeForParameters(kvp.Value.ToString())))
                       .JoinString("&");
        }

        internal static Dictionary<string, object> ApplyParameter(
            [NotNull] this Dictionary<string, object> dict, [CanBeNull] ParameterBase paramOrNull)
        {
            if (paramOrNull != null)
            {
                paramOrNull.SetDictionary(dict);
            }
            return dict;
        }

        private static string EncodeForParameters(string value)
        {
            var result = new StringBuilder();
            var data = Encoding.UTF8.GetBytes(value);
            var len = data.Length;

            for (var i = 0; i < len; i++)
            {
                int c = data[i];
                if (c < 0x80 && ParameterAllowedChars.IndexOf((char)c) != -1)
                {
                    result.Append((char)c);
                }
                else
                {
                    result.Append('%' + String.Format("{0:x2}", (int)data[i]));
                }
            }
            return result.ToString();
        }
    }
}

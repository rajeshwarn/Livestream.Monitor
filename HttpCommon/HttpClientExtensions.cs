﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HttpCommon
{
    public static class HttpClientExtensions
    {
        /// <summary> Creates a new HttpClient with compression enabled </summary>
        public static HttpClient CreateCompressionHttpClient()
        {
            return new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
        }

        public static async Task<T> ExecuteRequest<T>(string request)
        {
            return await ExecuteRequest<T>(CreateCompressionHttpClient(), request);
        }

        public static async Task<T> ExecuteRequest<T>(this HttpClient httpClient, string request)
        {
            if (string.IsNullOrWhiteSpace(request)) throw new ArgumentNullException(nameof(request));
            
            using (httpClient)
            {
                var httpResponseMessage = await httpClient.GetAsync(request);
                await httpResponseMessage.EnsureSuccessStatusCodeAsync();
                var response = await httpResponseMessage.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(response);
            }
        }

        private static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            // preserve the original content as the error message
            var content = await response.Content.ReadAsStringAsync();

            response.Content?.Dispose();
            throw new HttpRequestWithStatusException(response.StatusCode, content);
        }
    }
}
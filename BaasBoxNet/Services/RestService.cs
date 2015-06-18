﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaasBoxNet.Models;
using Newtonsoft.Json;

namespace BaasBoxNet.Services
{
    internal class RestService
    {
        private readonly BaasBox _box;

        internal RestService(BaasBox box)
        {
            _box = box;
        }

        public async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
        {
            using (var client = GetHttpClient())
            {
                using (var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false))
                {
                    return await ProcessResponse<T>(response);
                }
            }
        }

        public Task<T> PostAsync<T>(string url, object data, CancellationToken cancellationToken)
        {
            using (var client = GetHttpClient())
            {
                return MakeRestCallAsync<T>(client.PostAsync, url, data, cancellationToken);
            }
        }

        public Task<T> PutAsync<T>(string url, object data, CancellationToken cancellationToken)
        {
            using (var client = GetHttpClient())
            {
                return MakeRestCallAsync<T>(client.PutAsync, url, data, cancellationToken);
            }
        }

        private async Task<T> MakeRestCallAsync<T>(
            Func<string, HttpContent, CancellationToken, Task<HttpResponseMessage>> restMethod,
            string url,
            object data,
            CancellationToken cancellationToken)
        {
            StringContent requestBody = null;
            if (data != null)
            {
                var jsonData = JsonConvert.SerializeObject(data);
                requestBody = new StringContent(jsonData, Encoding.UTF8, "application/json");
            }
            using (requestBody)
            using (
                var response =
                    await restMethod(CreateRequestUrl(url), requestBody, cancellationToken).ConfigureAwait(false))
            {
                return await ProcessResponse<T>(response).ConfigureAwait(false);
            }
        }

        private async Task<T> ProcessResponse<T>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var parsedResponse = JsonConvert.DeserializeObject<BaasBoxResponse<T>>(content);
            return parsedResponse.Data;
        }

        private string CreateRequestUrl(string url)
        {
            return string.Format("{0}:{1}/{2}", _box.Config.ApiDomain, _box.Config.HttpPort, url);
        }

        private HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient {BaseAddress = new Uri(_box.Config.ApiDomain)};
            httpClient.DefaultRequestHeaders.Add("Content-type", "application/json");
            httpClient.DefaultRequestHeaders.Add("X-BAASBOX-APPCODE", _box.Config.AppCode);
            if (_box.UserManagement.IsAuthenticated)
                httpClient.DefaultRequestHeaders.Add("X-BB-SESSION", _box.User.Session);
            return httpClient;
        }
    }
}
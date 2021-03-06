﻿using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using unirest_net.request;

namespace unirest_net.http {
	public class HttpClientHelper
    {
        private const string USER_AGENT = "unirest-net/1.0";

        public static HttpResponse<T> Request<T>(HttpRequest request)
        {
            var response = RequestHelper(request).Result;

            return new HttpResponse<T>(response);
        }
        
        public static async Task<HttpResponse<T>> RequestAsync<T>(HttpRequest request)
        {
            return new HttpResponse<T>(await RequestHelper(request));
        }

        public static HttpResponse<T> RequestStream<T>(HttpRequest request)
        {
            var responseTask = RequestStreamHelper(request);
            Task.WaitAll(responseTask);
            var response = responseTask.Result;

            return new HttpResponse<T>(response);
        }

        public static async Task<HttpResponse<T>> RequestStreamAsync<T>(HttpRequest request)
        {
            return new HttpResponse<T>(await RequestStreamHelper(request));
        }

        private static async Task<HttpResponseMessage> RequestHelper(HttpRequest request)
        {
			//create http request
			using (HttpClient client = new HttpClient()) {
				client.Timeout = request.TimeOut;
				HttpRequestMessage msg = prepareRequest(request, client);
				return await client.SendAsync(msg);
			}
				
        }

        private static async Task<HttpResponseMessage> RequestStreamHelper(HttpRequest request)
        {
			//create http request
			using (HttpClient client = new HttpClient()) {
				client.Timeout = request.TimeOut;
				HttpRequestMessage msg = prepareRequest(request, client);

				client.Timeout = TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite);
				return await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
			}
        }


        private static HttpRequestMessage prepareRequest(HttpRequest request, HttpClient client)
        {
            if (!request.Headers.ContainsKey("user-agent"))
            {
                request.Headers.Add("user-agent", USER_AGENT);
            }

            //create http request
            HttpRequestMessage msg = new HttpRequestMessage(request.HttpMethod, request.URL);

            //process basic authentication
            if (request.NetworkCredentials != null)
            {
                string authToken = Convert.ToBase64String(
                                    UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}",
                                        request.NetworkCredentials.UserName,
                                        request.NetworkCredentials.Password))
                                    );

                string authValue = string.Format("Basic {0}", authToken);

                request.Headers.Add("Authorization", authValue);
            }

            //append body content
            if (request.Body != null)
            {
                if (!(request.Body is MultipartFormDataContent) || (request.Body as MultipartFormDataContent).Any())
                    msg.Content = request.Body;
            }

            //append all headers
            foreach (var header in request.Headers)
            {
                string contentTypeKey = "Content-type";
                if (header.Key.Equals(contentTypeKey, StringComparison.CurrentCultureIgnoreCase))
                {
                    msg.Content.Headers.Remove(contentTypeKey);
                    msg.Content.Headers.Add(contentTypeKey, header.Value);
                }
                else
                {
                    msg.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            //process message with the filter before sending
            if (request.Filter != null)
            {
                request.Filter(msg);
            }

            return msg;
        }
    }
}

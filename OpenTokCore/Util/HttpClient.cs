using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using Newtonsoft.Json;

using OpenTokSDK.Constants;
using OpenTokSDK.Exception;
using System.Net.Http;
using System.Xml;
using System.Net;
using System.Threading.Tasks;

namespace OpenTokSDK.Util
{
    /**
     * For internal use.
     */
    public class HttpClient
    {
        private string userAgent;
        private int apiKey;
        private string apiSecret;
        private string server;

        public HttpClient()
        {
            // This is only for testing purposes
        }

        public HttpClient(int apiKey, string apiSecret, string apiUrl = "")
        {
            this.apiKey = apiKey;
            this.apiSecret = apiSecret;
            this.server = apiUrl;
            this.userAgent = OpenTokVersion.GetVersion();
        }

        public virtual string Get(string url)
        {
            return Get(url, new Dictionary<string, string>());
        }

        public virtual string Get(string url, Dictionary<string, string> headers)
        {
            headers.Add("Method", "GET");
            return DoRequest(url, headers, null);
        }

        public virtual string Post(string url, Dictionary<string, string> headers, Dictionary<string, object> data)
        {
            headers.Add("Method", "POST");
            return DoRequest(url, headers, data);
        }

        public virtual string Delete(string url, Dictionary<string, string> headers, Dictionary<string, object> data)
        {
            headers.Add("Method", "DELETE");
            return DoRequest(url, headers, data);
        }

        public string DoRequest(string url, Dictionary<string, string> specificHeaders,
                                        Dictionary<string, object> bodyData)
        {
            Uri uri = new Uri(string.Format("{0}/{1}", server, url));

            string data = GetRequestPostData(bodyData, specificHeaders);
            var headers = GetRequestHeaders(specificHeaders);

            var t = MakeAsyncRequest(uri.ToString(), "application/json", "GET", headers, data);
            return t.Result;
        }

		public static Task<string> MakeAsyncRequest(string url, 
                                                    string contentType, 
                                                    string method, 
                                                    Dictionary<string, string> specificHeaders,
										            string data)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            string meth = "";
            foreach(var r in specificHeaders)
            {
                if(r.Key == "Method")
                {
                    meth = r.Value;
                }
            }
			request.ContentType = contentType;
            request.Method = meth;
			request.Proxy = null;
            foreach(var i in specificHeaders)
            {
                request.Headers[i.Key] = i.Value;
            }

            Task<System.IO.Stream> t = Task.Factory.FromAsync(
                request.BeginGetRequestStream, 
                asyncResult => request.EndGetRequestStream(asyncResult), 
                (object)null);

            t.ContinueWith(m => WriteRequestStream(m.Result, data));


			Task<WebResponse> task = Task.Factory.FromAsync(
				request.BeginGetResponse,
				asyncResult => request.EndGetResponse(asyncResult),
				(object)null);

			return task.ContinueWith(k => ReadStreamFromResponse(k.Result));
		}

        private static void WriteRequestStream(System.IO.Stream stream, string bodyData)
        {
            using (StreamWriter s = new StreamWriter(stream))
		    {
		        s.Write(bodyData);
		    }
		}

		private static string ReadStreamFromResponse(WebResponse response)
		{
			using (Stream responseStream = response.GetResponseStream())
			using (StreamReader sr = new StreamReader(responseStream))
			{
				//Need to return this response 
				string strContent = sr.ReadToEnd();
				return strContent;
			}
		}

        public XmlDocument ReadXmlResponse(string xml)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            return xmlDoc;
        }

        private HttpWebRequest CreateRequest(string url, Dictionary<string, string> headers, string data)
        {
            Uri uri = new Uri(string.Format("{0}/{1}", server, url));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            if (headers.ContainsKey("Content-type"))
            {
                request.ContentType = headers["Content-type"];
                headers.Remove("Content-type");
            }
            if (headers.ContainsKey("Method"))
            {
                request.Method = headers["Method"];
                headers.Remove("Method");
            }

            foreach (KeyValuePair<string, string> entry in headers)
            {
                request.Headers[entry.Key] =  entry.Value;
            }

            return request;
        }
        private Dictionary<string, string> GetRequestHeaders(Dictionary<string, string> headers)
        {
            var requestHeaders = GetCommonHeaders();
            requestHeaders = requestHeaders.Concat(headers).GroupBy(d => d.Key)
                                .ToDictionary(d => d.Key, d => d.First().Value);
            return requestHeaders;
        }

        private string GetRequestPostData(Dictionary<string, object> data, Dictionary<string, string> headers)
        {
            if (data != null && headers.ContainsKey("Content-type"))
            {
                if (headers["Content-type"] == "application/json")
                {
                    return JsonConvert.SerializeObject(data);
                }
                else if (headers["Content-type"] == "application/x-www-form-urlencoded")
                {
                    return ProcessParameters(data);
                }
            }
            else if (data != null || headers.ContainsKey("Content-type"))
            {
                throw new OpenTokArgumentException("If Content-type is set in the headers data in the body is expected");
            }
            return "";
        }

        private string ProcessParameters(Dictionary<string, object> parameters)
        {
            string data = string.Empty;

            foreach (KeyValuePair<string, object> pair in parameters)
            {
                data += pair.Key + "=" + System.Uri.EscapeUriString(pair.Value.ToString()) + "&";
            }
            return data.Substring(0, data.Length - 1);
        }
        private Dictionary<string, string> GetCommonHeaders()
        {
            return new Dictionary<string, string> 
            {   { "X-TB-PARTNER-AUTH", String.Format("{0}:{1}", apiKey, apiSecret) },            
                { "X-TB-VERSION", "1" },
            };
        }
    }
}

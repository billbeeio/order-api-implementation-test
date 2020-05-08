using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serializers;

namespace Billbee.MinimalOrderApi
{
      public delegate string PreProcessString(string xml);


    public class RestJsonSerializer<T> : ISerializer
    {
        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }
        public string ContentType { get; set; }

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
    }


    public class RestXmlSerializer<T> : ISerializer
    {

        System.Xml.Serialization.XmlSerializer mySerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

        public string ContentType
        {
            get
            {
                return "text/xml; charset=UTF-8";
            }

            set
            {

            }
        }

        public string DateFormat
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Namespace
        {
            get
            {
                return "";
            }

            set
            {
            }
        }

        public string RootElement
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }



        public string Serialize(object obj)
        {
            if (obj is T)
            {
                string retStr = "";
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms, new UTF8Encoding(false)))
                    {
                        mySerializer.Serialize(sw, obj);
                        retStr = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }

                return StripNullableEmptyXmlElements(retStr, false);
            }
            else
            {
                throw new Exception($"Wrong datatype supplied. Expected {typeof(T).Name}, but got {obj.GetType().Name}");
            }
        }

        public string StripNullableEmptyXmlElements(string input, bool compactOutput = false)
        {
            const RegexOptions OPTIONS =
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;

            var result = Regex.Replace(
                input,
                @"<\w+\s+\w+:nil=""true""(\s+xmlns:\w+=""http://www.w3.org/2001/XMLSchema-instance"")?\s*/>",
                string.Empty,
                OPTIONS
            );

            if (compactOutput)
            {
                var sb = new StringBuilder();

                using (var sr = new StringReader(result))
                {
                    string ln;

                    while ((ln = sr.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(ln))
                        {
                            sb.AppendLine(ln);
                        }
                    }
                }

                result = sb.ToString();
            }

            return result;
        }
    }

    public abstract class RestClientBaseClass
    {
        public ILogger Logger { get; set; }
        protected abstract string BaseUrl { get; }

        public bool IgnoreNullFields { get; set; } = false;

        protected virtual IAuthenticator Authenticator { get; set; }
        public virtual Dictionary<string, string> AdditionalHeaders { get; protected set; }

        private string logCtx;

        protected virtual WebProxy Proxy { get; }

        public DataFormat RequestFormat { get; set; } = DataFormat.Json;


        protected RestClientBaseClass(ILogger logger, string logCtx)
        {
            Logger = logger;
            this.logCtx = logCtx;
        }

        protected string put(string resource, NameValueCollection parameter = null)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter);
            var response = c.Put(req);

            throwWhenErrResponse(response, resource);
            return response.Content;
        }

        protected T put<T>(string resource, NameValueCollection parameter = null) where T : new()
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter);
            var response = c.Put<T>(req);

            throwWhenErrResponse(response, resource);
            return response.Data;
        }

        protected T put<T>(string resource, dynamic data, NameValueCollection parameter = null, Action<IRestRequest> preRequestHook = null, bool useNewtonSoft = false) where T : new()
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter);
            
            if (useNewtonSoft)
            {
                req.JsonSerializer = new RestJsonSerializer<object>
                {
                    ContentType = "application/json",
                };
            }
            
            if (data != null)
                req.AddBody(data);

            preRequestHook?.Invoke(req);
            var response = c.Put<T>(req);

            throwWhenErrResponse(response, resource);
            return response.Data;
        }

        protected string put(string resource, NameValueCollection parameter = null, List<FileParam> files = null, ParameterType paramType = ParameterType.QueryString)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter, paramType);

            if (files != null)
            {
                foreach (var f in files)
                {
                    req.AddFile(f.Name, f.Data, f.FileName, f.ContentType);
                }
            }

            var response = c.Put(req);
            throwWhenErrResponse(response, resource);
            return response.Content;
        }

        protected T put<T>(string resource, NameValueCollection parameter, List<FileParam> files, ParameterType paramType = ParameterType.QueryString)
        {
            var resStr = put(resource, parameter, files, paramType);
            return JsonConvert.DeserializeObject<T>(resStr.TrimStart((char)65279));
        }

        protected string patch(string resource, NameValueCollection parameter = null, dynamic data = null)
        {
            var response = patchForResponse(resource, parameter, data);
            return response.Content;
        }

        protected IRestResponse<T> PatchForResponse<T>(string resource, NameValueCollection parameter = null,
            dynamic data = null, bool throwOnError = true) where T : new()
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter);
            if (data != null)
            {
                req.AddBody(data);
            }
            var response = c.Patch<T>(req);

            if (throwOnError)
                throwWhenErrResponse(response, resource);
            return response;
        }

        protected IRestResponse patchForResponse(string resource, NameValueCollection parameter = null, dynamic data = null, bool throwOnError = true)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter);
            if (data != null)
            {

                req.AddBody(data);
            }
            var response = c.Patch(req);

            if (throwOnError)
                throwWhenErrResponse(response, resource);
            return response;
        }

        protected T patch<T>(string resource, NameValueCollection parameter = null, dynamic data = null, bool useNewtonSoft = false) where T : new()
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter);
            
            if (useNewtonSoft)
            {
                req.JsonSerializer = new RestJsonSerializer<object>
                {
                    ContentType = "application/json",
                };
            }

            if (data != null)
            {
                req.AddBody(data);
            }
            var response = c.Patch<T>(req);

            throwWhenErrResponse(response, resource);
            return response.Data;
        }

        protected virtual string parseError(RestSharp.IRestResponse response)
        {
            string errMsg = null;
            try
            {
                var errResponse = JsonConvert.DeserializeObject<JObject>(response.Content.TrimStart((char)65279));
                errMsg = errResponse?["code"]?.Value<string>() + " - " + errResponse?["message"]?.Value<string>();

                // debitoor
                var errArr = errResponse?["errors"];
                if (errArr != null)
                {
                    errMsg += string.Join(", ", errArr.Select(e => $" prop: {e["property"]?.Value<string>()} msg: {e["message"]?.Value<string>()}"));
                }
            }
            catch
            {
            }

            errMsg = errMsg ?? ($"Anfrage fehlgeschlagen: {response.StatusCode} {response.StatusDescription}");

            return errMsg;
        }

        protected virtual void throwWhenErrResponse(RestSharp.IRestResponse response, string resource)
        {
            if (response.StatusCode != HttpStatusCode.OK
                && response.StatusCode != HttpStatusCode.Created
                && response.StatusCode != HttpStatusCode.Accepted
                && response.StatusCode != HttpStatusCode.NoContent
                )
            {
                var errMsg = parseError(response);

                if (string.IsNullOrWhiteSpace(errMsg))
                    errMsg = $"{response.StatusDescription}";
                    
                if (Logger != null)
                {
                    Logger.Error($"Request to {resource} failed: " + errMsg, logCtx);
                    Logger.Error($"Request to {resource} failed: " + response.Content, logCtx);
                }
                
                throw new RestResponseException(errMsg)
                {
                    Response = response,
                    StatusCode = response.StatusCode,
                    StatusDescription = response.StatusDescription
                };
            }
        }
        
        public class RestResponseException : Exception
        {
            public RestResponseException(string msg) : base(msg)
            { }

            public HttpStatusCode StatusCode { get; set; }
            public string StatusDescription { get; set; }
            public IRestResponse Response { get; set; }
        }


        protected class FileParam
        {
            public string Name { get; set; }
            public string FileName { get; set; }
            public byte[] Data { get; set; }
            public string ContentType { get; set; }
        }

        protected async Task<string> postAsync(string resource, NameValueCollection parameter = null, List<FileParam> files = null, ParameterType paramType = ParameterType.QueryString, string acceptHeaderValue = "application/json")
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter, paramType, acceptHeaderValue);

            if (files != null)
            {
                foreach (var f in files)
                {
                    req.AddFile(f.Name, f.Data, f.FileName, f.ContentType);
                }
            }

            var response = await c.ExecutePostTaskAsync(req).ConfigureAwait(false);
            throwWhenErrResponse(response, resource);
            return response.Content;
        }

        protected void delete(string resource, NameValueCollection parameter = null, ParameterType paramType = ParameterType.QueryString)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter, paramType);
            var response = c.Delete(req);
            throwWhenErrResponse(response, resource);
        }

        protected string post(string resource, NameValueCollection parameter = null, List<FileParam> files = null, ParameterType paramType = ParameterType.QueryString)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter, paramType);

            if (files != null)
            {
                foreach (var f in files)
                {
                    req.AddFile(f.Name, f.Data, f.FileName, f.ContentType);
                }
            }

            var response = c.Post(req);
            throwWhenErrResponse(response, resource);
            return response.Content;
        }

        protected T post<T>(string resource, NameValueCollection parameter = null, List<FileParam> files = null, ParameterType paramType = ParameterType.QueryString)
        {
            var resStr = post(resource, parameter, files, paramType);
            return JsonConvert.DeserializeObject<T>(resStr.TrimStart((char)65279));
        }

        protected async Task<string> postAsync(string resource, dynamic data)
        {
            var response = await postAsyncForResponse(resource, data).ConfigureAwait(false);
            return response.Content;
        }

        protected async Task<IRestResponse> postAsyncForResponse(string resource, dynamic data)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);
            req.AddBody(data);
            var response = await c.ExecutePostTaskAsync(req).ConfigureAwait(false);
            throwWhenErrResponse(response, resource);
            return response;
        }

        protected async Task<T> postAsync<T>(string resource, dynamic data, NameValueCollection parameter = null, ParameterType paramType = ParameterType.QueryString) where T : new()
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter);
            if (data != null)
                req.AddBody(data);
            var response = await c.ExecutePostTaskAsync<T>(req).ConfigureAwait(false);
            throwWhenErrResponse(response, resource);
            return response.Data;
        }

        protected string post(string resource, dynamic data, Action<IRestRequest> preRequestHook = null)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);
            if (data != null)
                req.AddBody(data);
            preRequestHook?.Invoke(req);
            var response = c.Post(req);
            throwWhenErrResponse(response, resource);
            return response.Content;
        }

        protected U postXml<T, U>(string resource, T data, PreProcessString preprocessor = null)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);
            req.RequestFormat = DataFormat.Xml;
            req.XmlSerializer = new RestXmlSerializer<T>();
            req.AddXmlBody(data);

            var response = c.Post(req);
            throwWhenErrResponse(response, resource);

            string processedContent = preprocessor != null ? preprocessor(response.Content) : response.Content;

            using (MemoryStream reader = new MemoryStream(Encoding.UTF8.GetBytes(processedContent)))
            {
                System.Xml.Serialization.XmlSerializer respSer = new System.Xml.Serialization.XmlSerializer(typeof(U));
                return (U)respSer.Deserialize(reader);
            }
        }

        protected U getXml<U>(string resource, string baseUrl = "", bool GetRawContent = false)
        {
            var c = createRestClient();
            c.BaseUrl = String.IsNullOrWhiteSpace(baseUrl) ? c.BaseUrl : new Uri(baseUrl);

            var req = createRestRequest(resource, null);
            req.RequestFormat = DataFormat.Xml;
            var response = c.Get(req);
            throwWhenErrResponse(response, resource);

            if (!GetRawContent)
                using (MemoryStream reader = new MemoryStream(Encoding.UTF8.GetBytes(response.Content)))
                {
                    System.Xml.Serialization.XmlSerializer respSer = new System.Xml.Serialization.XmlSerializer(typeof(U));
                    return (U)respSer.Deserialize(reader);
                }
            else
                using (MemoryStream reader = new MemoryStream(response.RawBytes))
                {
                    System.Xml.Serialization.XmlSerializer respSer = new System.Xml.Serialization.XmlSerializer(typeof(U));
                    return (U)respSer.Deserialize(reader);
                }
        }

        protected async Task<U> postXmlAsync<T, U>(string resource, T data)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);

            req.RequestFormat = DataFormat.Xml;
            req.XmlSerializer = new RestXmlSerializer<T>();
            req.AddXmlBody(data);

            var response = await c.ExecutePostTaskAsync<T>(req).ConfigureAwait(false);
            throwWhenErrResponse(response, resource);

            using (MemoryStream reader = new MemoryStream(Encoding.UTF8.GetBytes(response.Content)))
            {
                System.Xml.Serialization.XmlSerializer respSer = new System.Xml.Serialization.XmlSerializer(typeof(U));
                return (U)respSer.Deserialize(reader);
            }
        }

        public class FileForUpload
        {
            public byte[] Data { get; set; }
            public string FieldName { get; set; }
            public string Name { get; set; }
            public bool AsParameter { get; set; } = false;
            public string ContentType { get; set; }
        }

        protected T post<T>(string resource, 
                            dynamic data, 
                            bool useNewtonSoft = false, 
                            Action<IRestResponse<T>> preDeserializeHook = null,
                            List<FileForUpload> files = null) where T : new()
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);
            if (useNewtonSoft)
            {
                req.JsonSerializer = new RestJsonSerializer<object>
                {
                    ContentType = "application/json",
                };
            }

            if (data != null)
            {
                req.AddBody(data);
            }
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (!file.AsParameter)
                    {
                        req.AddFile(file.FieldName, file.Data, file.Name, file.ContentType);
                    }
                    else
                    {
                        req.AddParameter(file.FieldName, Encoding.UTF8.GetString(file.Data));
                    }
                }
            }
            
            var response = c.Post<T>(req);
            preDeserializeHook?.Invoke(response);

            throwWhenErrResponse(response, resource);

            return useNewtonSoft
                ? JsonConvert.DeserializeObject<T>(response.Content.TrimStart((char)65279))
                : response.Data;
        }

        protected IRestResponse<T> PostRawResult<T>(string resource, dynamic data, bool holdOnError = true) where T : new()
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);
            req.AddBody(data);
            var response = c.Post<T>(req);

            if (holdOnError)
                throwWhenErrResponse(response, resource);

            return response;
        }


        protected async Task<string> putAsync(string resource, NameValueCollection parameter = null)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, parameter);
            req.Method = Method.PUT;
            var response = await c.ExecuteTaskAsync(req).ConfigureAwait(false);
            throwWhenErrResponse(response, resource);
            return response.Content;
        }

        protected async Task<string> putAsync(string resource, dynamic data)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);
            req.Method = Method.PUT;
            req.AddBody(data);

            var response = await c.ExecuteTaskAsync(req).ConfigureAwait(false);
            throwWhenErrResponse(response, resource);
            return response.Content;
        }

        protected async Task<HttpStatusCode> putAsyncGetStatus(string resource, dynamic data)
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);
            req.Method = Method.PUT;
            req.AddBody(data);

            var response = await c.ExecuteTaskAsync(req).ConfigureAwait(false);
            throwWhenErrResponse(response, resource);
            return response.StatusCode;
        }

        protected async Task<T> putAsync<T>(string resource, dynamic data) where T : new()
        {
            var c = createRestClient();
            var req = createRestRequest(resource, null);
            req.Method = Method.PUT;
            req.AddBody(data);
            var response = await c.ExecuteTaskAsync<T>(req).ConfigureAwait(false);
            throwWhenErrResponse(response, resource);
            return response.Data;
        }

        /// <summary>
        /// Retries a network request up to 4 times when throttled
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resource"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private IRestResponse<T> retry<T>(string resource, Func<IRestResponse<T>> action, int sleepTimeMs)
        {
            IRestResponse<T> response = null;
            for (byte retryCounter = 5; retryCounter > 0; retryCounter--)
            {
                response = action();

                if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                {
                    // Throttling
                    Thread.Sleep(sleepTimeMs);
                }
                else
                {
                    throwWhenErrResponse(response, resource);
                    return response;
                }
            }
            throwWhenErrResponse(response, resource);
            throw new Exception("Request is throttled");
        }

        private async Task<IRestResponse<T>> retryAsync<T>(string resource, Func<Task<IRestResponse<T>>> action, int sleepTimeMs)
        {
            IRestResponse<T> response = null;
            for (byte retryCounter = 5; retryCounter > 0; retryCounter--)
            {
                response = await action().ConfigureAwait(false);

                if ((int)response.StatusCode == 429)
                {
                    // Throttling
                    Thread.Sleep(sleepTimeMs);
                }
                else
                {
                    throwWhenErrResponse(response, resource);
                    return response;
                }
            }
            throwWhenErrResponse(response, resource);
            throw new Exception("Request is throttled");
        }

        private IRestResponse retry(string resource, Func<IRestResponse> action, int sleepTimeMs)
        {
            IRestResponse response = null;
            for (byte retryCounter = 5; retryCounter > 0; retryCounter--)
            {
                response = action();

                if ((int)response.StatusCode == 429)
                {
                    // Throttling
                    Thread.Sleep(sleepTimeMs);
                }
                else
                {
                    throwWhenErrResponse(response, resource);
                    return response;
                }
            }
            throwWhenErrResponse(response, resource);
            throw new Exception("Request is throttled");
        }

        protected async Task<T> getAsync<T>(string resource, NameValueCollection parameter = null, Action<IList<Parameter>> headerProcessor = null,
            int sleepTimeMs = 1000, Action<IRestResponse<T>> preDeserializeHook = null, NameValueCollection headerParameter = null)
            where T : new()
        {
            var req = prepareGetReq(resource, parameter, headerParameter);

            var c = createRestClient();

            IRestResponse<T> response = await retryAsync(resource, async () => await c.ExecuteGetTaskAsync<T>(req), sleepTimeMs).ConfigureAwait(false);
            headerProcessor?.Invoke(response.Headers);
            preDeserializeHook?.Invoke(response);
            var data = JsonConvert.DeserializeObject<T>(response.Content.TrimStart((char)65279));
            return data;
        }

        protected T get<T>(string resource, NameValueCollection parameter = null, Action<IList<Parameter>> headerProcessor = null,
            int sleepTimeMs = 1000, Action<IRestResponse<T>> preDeserializeHook = null, NameValueCollection headerParameter = null, string acceptHeaderValue = "application/json", string alternativeBaseUrl = null)
            where T : new()
        {
            var req = prepareGetReq(resource, parameter, headerParameter, acceptHeaderValue);
            var c = createRestClient(alternativeBaseUrl);

            IRestResponse<T> response = retry(resource, () => c.Get<T>(req), sleepTimeMs);
            headerProcessor?.Invoke(response.Headers);
            preDeserializeHook?.Invoke(response);

            if (response.ContentType == "text/xml")
            {
                return new XmlDeserializer().Deserialize<T>(response);
            }
            return JsonConvert.DeserializeObject<T>(response.Content.TrimStart((char)65279));
        }

        protected byte[] getRawBytes(string resource, NameValueCollection parameter = null, Action<IList<Parameter>> headerProcessor = null,
            int sleepTimeMs = 1000, NameValueCollection headerParameter = null, string acceptHeaderValue = "application/json")
        {
            var req = prepareGetReq(resource, parameter, headerParameter, acceptHeaderValue);
            
            var c = createRestClient();

            IRestResponse response = retry(resource, () => c.Get(req), sleepTimeMs);
            headerProcessor?.Invoke(response.Headers);
            return response.RawBytes;
        }

        protected string get(string resource, NameValueCollection parameter = null, Action<IList<Parameter>> headerProcessor = null,
            int sleepTimeMs = 1000, NameValueCollection headerParameter = null, string acceptHeaderValue = "application/json")
        {
            var req = prepareGetReq(resource, parameter, headerParameter, acceptHeaderValue);
            
            var c = createRestClient();

            IRestResponse response = retry(resource, () => c.Get(req), sleepTimeMs);
            headerProcessor?.Invoke(response.Headers);
            return response.Content;
        }

        private RestRequest prepareGetReq(string resource, NameValueCollection parameter = null, NameValueCollection headerParameter = null, string acceptHeaderValue = "application/json")
        {
            var req = createRestRequest(resource, parameter, acceptHeaderValue: acceptHeaderValue);

            if (headerParameter != null)
            {
                foreach (var param in headerParameter.AllKeys)
                {
                    req.AddHeader(param, headerParameter[param]);
                }
            }

            var pStr = parameter == null ? "" : string.Join(",", parameter.AllKeys.Select(k => $"{k}={parameter[k]}"));

            if (Logger != null)
                Logger.Debug($"Requesting {resource} with params {pStr}", logCtx);
            return req;
        }

        protected RestRequest createRestRequest(string resource, NameValueCollection parameter, ParameterType paramType = ParameterType.QueryString, string acceptHeaderValue = "application/json")
        {
            RestRequest restreq = new RestRequest(resource);
            restreq.AddHeader("Accept", acceptHeaderValue);
            restreq.RequestFormat = this.RequestFormat;

            if (IgnoreNullFields)
                restreq.JsonSerializer = new RestJsonSerializer<object>();

            if (parameter != null && parameter.Count > 0)
            {
                foreach (string key in parameter)
                {
                    restreq.AddParameter(key, parameter[key], paramType);
                }
            }
            return restreq;
        }

        protected RestClient createRestClient(string alternativeBaseUrl = null)
        {
            RestClient rc = new RestClient(alternativeBaseUrl ?? BaseUrl)
            {
                Authenticator = Authenticator,
            };

            if (Proxy != null) rc.Proxy = Proxy;

            if (AdditionalHeaders != null)
                foreach (var h in AdditionalHeaders)
                {
                    rc.AddDefaultHeader(h.Key, h.Value);
                }
            return rc;
        }
    }

    public interface ILogger
    {
        void Error(string errMsg, string logCtx);
        void Debug(string s, string logCtx);
    }

    public class ConsoleLogger : ILogger
    {
        public void Error(string errMsg, string logCtx)
        {
            Console.WriteLine($"Error {logCtx}: {errMsg}");
        }

        public void Debug(string s, string logCtx)
        {
            Console.WriteLine($"Debug {logCtx}: {s}");
        }
    }
}
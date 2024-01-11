using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace RequestResponseModule
{
    class Logger : IHttpModule {
        private NameValueCollection _appSettings = ConfigurationManager.AppSettings;

        public void Dispose() {

        }

        public void Init(HttpApplication context) {
            context.BeginRequest += new EventHandler(Context_BeginRequest);
            context.EndRequest += new EventHandler(Context_EndRequest);
        }

        private bool IsLoggable(HttpRequest request) {            
            bool result = false;

            string path = request.Url.AbsolutePath?.ToLower();

            // Don't log unless explicitly enabled in the config
            bool enabled = bool.Parse(_appSettings["requestResponseLogger.enabled"] ?? "False");

            if(!enabled) { 
                return false;
            }

            string[] filterPathInclude = _appSettings["requestResponseLogger.path.include"]?.Split(',');
            string[] filterPathExclude = _appSettings["requestResponseLogger.path.exclude"]?.Split(',');

            if(filterPathInclude.Length > 0) {                
                foreach(string s in filterPathInclude) {                        
                    if(path.Contains(s.Trim())) {
                        result = true;
                        break;
                    }
                }                
            }

            if(filterPathExclude.Length > 0) {
                foreach(string s in filterPathExclude) {
                    if(path.Contains(s.Trim())) {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        private void Context_BeginRequest(object sender, EventArgs e) {            
            HttpRequest request = HttpContext.Current.Request;            

            if(IsLoggable(request)) {
                HttpResponse response = HttpContext.Current.Response;

                // Add this Stream object to the response Filter so we can access the response output (response.OutputStream is not directly readable).
                OutputFilterStream filter = new OutputFilterStream(response.Filter);
                response.Filter = filter;

                // Add the filter to the Context.Items so we can access if again later
                HttpContext.Current.Items.Add("Filter", filter);
                // Flag the request start date so we can determine the processing duration
                HttpContext.Current.Items.Add("BeginRequest", DateTime.Now);
            }
        }        

        private void Context_EndRequest(object sender, EventArgs e) {
            HttpApplication httpApplication = (HttpApplication)sender;

            HttpRequest request = httpApplication.Request;
            HttpResponse response = httpApplication.Response;
            bool loggable = IsLoggable(request);
            try {
                if(loggable) {
                    string uri = request.Url.PathAndQuery.ToLower();
                    string method = request.HttpMethod.ToUpper();
                    string requestBody = null;
                    string responseBody = null;

                    if(response.Filter != null) {
                        OutputFilterStream filter = (OutputFilterStream)HttpContext.Current.Items["Filter"];
                        responseBody = filter?.ReadStream();
                    }

                    DateTime beginRequest = (DateTime)httpApplication.Context.Items["BeginRequest"];

                    LogEntry logEntry = null;

                    using(StreamReader srInput = new StreamReader(
                        request.InputStream,
                        request.ContentEncoding)) {
                        srInput.BaseStream.Seek(0, SeekOrigin.Begin);
                        requestBody = srInput.ReadToEnd() ?? String.Empty;
                    }

                    logEntry = new LogEntry() {
                        Url = uri,
                        RequestBody = requestBody,
                        HttpStatusCode = response.StatusCode,
                        Method = method,
                        TimeStamp = beginRequest,
                        ProcessingTime = DateTime.Now - beginRequest,
                        ResponseBody = responseBody ?? string.Empty
                    };

                    LogWriter.Instance.Write(logEntry.ToString());
                }
            } catch(Exception ex) {
                // Log the error to the event log
                using(EventLog eventLog = new EventLog("Application")) {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(
                        $"{this.GetType().FullName} - Unable to log for application {AppDomain.CurrentDomain.FriendlyName}: {ex.GetBaseException()}",
                        EventLogEntryType.Error);
                }
            } finally {
                response.Headers.Add("Loggable", loggable.ToString());
            }
        }        
    }
}

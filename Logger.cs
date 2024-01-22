using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace RequestResponseModule
{
    class Logger : IHttpModule {
        private NameValueCollection _appSettings = ConfigurationManager.AppSettings;

        public void Dispose() {

        }

        public void Init(HttpApplication context) {
            // Register our events and handlers
            context.BeginRequest += new EventHandler(Context_BeginRequest);
            context.EndRequest += new EventHandler(Context_EndRequest);
        }

        /// <summary>
        /// Determines whether this request is to be logged based on the config settings and values.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool IsLoggable(HttpRequest request) {            
            bool result = false;

            string path = request.Url.AbsolutePath?.ToLower();

            // Don't log unless explicitly enabled in the config
            // If the setting is missing, we don't log.
            bool enabled = bool.Parse(_appSettings["requestResponseLogger.enabled"] ?? "False");

            if(!enabled) { 
                return false;
            }

            // Grab the paths to include from the config file.
            string[] filterPathInclude = _appSettings["requestResponseLogger.path.include"]?.Split(',');
            // Grab the paths to exclude from the config file.
            string[] filterPathExclude = _appSettings["requestResponseLogger.path.exclude"]?.Split(',');

            // Check if we have a match on the "include" filter
            if(filterPathInclude.Length > 0) {                
                foreach(string s in filterPathInclude) {                        
                    if(path.Contains(s.Trim())) {
                        result = true;
                        // Exit the loop - no need for futher processing
                        break;
                    }
                }                
            }

            // Check if the path contains any of the "exlude" filter values
            if(filterPathExclude.Length > 0) {
                foreach(string s in filterPathExclude) {
                    if(path.Contains(s.Trim())) {
                        result = false;
                        // Exit the loop - no need for futher processing
                        break;
                    }
                }
            }

            return result;
        }

        private void Context_BeginRequest(object sender, EventArgs e) {            
            HttpRequest request = HttpContext.Current.Request;            

            // Determine if this request is to be logged
            if(IsLoggable(request)) {
                HttpResponse response = HttpContext.Current.Response;

                // Add this Stream object to the response Filter so we can access the response output (response.OutputStream is not directly readable).
                OutputFilterStream filter = new OutputFilterStream(response.Filter);
                response.Filter = filter;

                // Add the filter to the Context.Items so we can access it again later
                HttpContext.Current.Items.Add("Filter", filter);

                // Flag the request start date so we can determine the processing duration
                HttpContext.Current.Items.Add("BeginRequest", DateTime.Now);
            }
        }        

        private void Context_EndRequest(object sender, EventArgs e) {
            HttpApplication httpApplication = (HttpApplication)sender;

            HttpRequest request = httpApplication.Request;
            HttpResponse response = httpApplication.Response;

            // Determine if this request is to be logged
            bool loggable = IsLoggable(request);

            try {
                if(loggable) {
                    string uri = request.Url.PathAndQuery.ToLower();
                    string method = request.HttpMethod.ToUpper();
                    string requestBody = null;
                    string responseBody = null;

                    if(response.Filter != null) {
                        // Use the OutputFilterStream object that was saved to
                        // HttpContext.Current.Items to get a copy of the response stream.
                        OutputFilterStream filter = (OutputFilterStream)HttpContext.Current.Items["Filter"];
                        responseBody = filter?.ReadStream();
                    }

                    DateTime beginRequest = (DateTime)httpApplication.Context.Items["BeginRequest"];

                    LogEntry logEntry = null;

                    // Get the request body
                    using(StreamReader srInput = new StreamReader(
                        request.InputStream,
                        request.ContentEncoding)) {
                        srInput.BaseStream.Seek(0, SeekOrigin.Begin);
                        requestBody = srInput.ReadToEnd() ?? String.Empty;
                    }

                    // Create a new LogEntry object and assign the values
                    logEntry = new LogEntry() {
                        Url = uri,
                        RequestBody = requestBody,
                        HttpStatusCode = response.StatusCode,
                        Method = method,
                        TimeStamp = beginRequest,
                        ProcessingTime = DateTime.Now - beginRequest,
                        ResponseBody = responseBody ?? string.Empty
                    };

                    // Run the IO on a separate thread so
                    // we don't block the main thread.
                    Task.Run(() => {
                        LogWriter.Instance.Write(logEntry.ToString());
                    }).ConfigureAwait(false);                    
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
                // Add the custom response header to indicate
                // if the request qualified to be logged.
                response.Headers.Add("Loggable", loggable.ToString());
            }
        }        
    }
}

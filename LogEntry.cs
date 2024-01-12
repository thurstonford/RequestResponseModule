using System;
using System.Text;
using System.Web;

namespace RequestResponseModule
{
    public class LogEntry
	{     
        /// <summary>
        /// The URL path that was requested.
        /// </summary>
        public string Url { get; set; }
             
        /// <summary>
        /// The HTTP verb (GET, POST, PUT, DELETE etc)
        /// </summary>
        public string Method { get; set; }
		
        /// <summary>
        /// The date and time that the request was received by the application
        /// </summary>
		public DateTime TimeStamp { get; set; }        	

        /// <summary>
        /// The contents of the request body, if any.
        /// </summary>
		public string RequestBody { get; set; }
                
        /// <summary>
        /// The contents of the response body, if any.
        /// </summary>
        public string ResponseBody { get; set; }
                
        /// <summary>
        /// TimeSpan that indicates how long it took to process the request.
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }
                
        /// <summary>
        /// The response HTTP status code.
        /// </summary>
        public int HttpStatusCode { get; set; }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"{nameof(Url)}\":\"{Url}\",");
            sb.Append($"\"{nameof(Method)}\":\"{Method}\",");
            sb.Append($"\"{nameof(TimeStamp)}\":\"{TimeStamp:yyyy-MM-dd HH:mm:ss.fff}\",");                        
            sb.Append($"\"{nameof(RequestBody)}\":\"{(Utils.IsJson(RequestBody) ? HttpUtility.JavaScriptStringEncode(RequestBody) : HttpUtility.HtmlEncode(RequestBody))}\",");
            sb.Append($"\"{nameof(ResponseBody)}\":\"{(Utils.IsJson(ResponseBody) ? HttpUtility.JavaScriptStringEncode(ResponseBody) : HttpUtility.HtmlEncode(ResponseBody))}\",");            
            sb.Append($"\" {nameof(ProcessingTime)} \":\"{ProcessingTime:c}\",");
            sb.Append($"\"{nameof(HttpStatusCode)}\":{HttpStatusCode}");
            sb.Append("}");

            // Feeble attempt at minifying
            sb.Replace(Environment.NewLine, string.Empty).Replace("\t", string.Empty).Replace("    ", string.Empty);

            return sb.ToString();
        }       
    }
}
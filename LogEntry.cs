using System;
using System.Text;
using System.Web;

namespace RequestResponseModule
{
    public class LogEntry
	{        
        public string Url { get; set; }
             
        public string Method { get; set; }
		
		public DateTime TimeStamp { get; set; }
        		
		public string RequestBody { get; set; }
                
        public string ResponseBody { get; set; }
                
        public TimeSpan ProcessingTime { get; set; }
                
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
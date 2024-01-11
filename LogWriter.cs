using System;
using System.IO;
using System.Threading;
using System.Web;

namespace RequestResponseModule
{
    public class LogWriter
    {
        private static LogWriter _instance;
        private static readonly string _rootPath = HttpContext.Current.Server.MapPath("~/");
        private static readonly string _logPath = _rootPath + "Logs\\RequestResponse\\";
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);        
        private FileStream _fileStream;
        private StreamWriter _streamWriter;
        private static SemaphoreSlim _errorsSemaphore = new SemaphoreSlim(1, 1);
        private FileStream _errorsFileStream;
        private StreamWriter _errorsStreamWriter;
        private static readonly string _errorsFilePath = $"{_logPath}\\Errors.log";

        public static LogWriter Instance
        {
            get
            {                
                if (_instance == null) {
                    _instance = new LogWriter();
                }              
                return _instance;
            }
        }

        private string FilePath { 
            get { 
                return $"{_logPath}\\{DateTime.Now:yyyyMMdd}.json"; 
            } 
        }
        private LogWriter() {            
            InitializeLog();
            InitializeErrorLog();
        }

        private void InitializeLog() {
            try {                                
                if(!Directory.Exists(_logPath)) {
                    Directory.CreateDirectory(_logPath);
                }

                _fileStream = new FileStream(FilePath, FileMode.OpenOrCreate);
                _streamWriter = new StreamWriter(_fileStream);                
            } catch(Exception ex) {
                LogErrors(ex);
            }
        }

        private void InitializeErrorLog() {
            try {
                if(!Directory.Exists(_logPath)) {
                    Directory.CreateDirectory(_logPath);
                }

                _errorsFileStream = new FileStream(_errorsFilePath, FileMode.OpenOrCreate);
                _errorsStreamWriter = new StreamWriter(_errorsFileStream);
            } catch(Exception ex) {
                LogErrors(ex);
            }
        }

        public void Write(string message)
        {
            try {
                _semaphore.Wait();

                // We may have rolled over into a new day so create a
                // new log file and dereference the previous log file.
                if(!File.Exists(FilePath)) {
                    try { 
                        _streamWriter?.Close();
                        _fileStream?.Close();
                    } catch(Exception ex) {
                        LogErrors(ex);
                    }
                }

                if(_fileStream == null || _streamWriter == null) {
                    InitializeLog();
                }

                //write to the last line                
                _fileStream.Seek(0, SeekOrigin.End);
                _streamWriter.WriteLine(message);
                _streamWriter.Flush();

                _semaphore.Release();
            } catch(Exception ex) {
                _semaphore.Release();
                LogErrors(ex);
            }
        }

        public void LogErrors(Exception ex) {
            try {
                _errorsSemaphore.Wait();
                
                if(_errorsFileStream == null || _errorsStreamWriter == null) {
                    InitializeErrorLog();
                }
                                
                _errorsFileStream.Seek(0, SeekOrigin.End);
                _errorsStreamWriter.WriteLine($"ERROR - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} - {ex.GetBaseException()}");
                _errorsStreamWriter.Flush();

                _errorsSemaphore.Release();
            } catch(Exception) {
                _errorsSemaphore.Release();
            }             
        }
    }
}
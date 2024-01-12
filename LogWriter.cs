using System;
using System.IO;
using System.Threading;
using System.Web;

namespace RequestResponseModule
{
    public class LogWriter
    {
        // Holds the instance of the Singleton
        private static LogWriter _instance;
        private static readonly string _rootPath = HttpContext.Current.Server.MapPath("~/");
        private static readonly string _logPath = _rootPath + "Logs\\RequestResponse\\";        
        // Used to synchronize writing to the log file.
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);        
        // FileStream object used when writing to the log file.
        private FileStream _fileStream;
        // Streamwriter object used when writing to the log file.
        private StreamWriter _streamWriter;
        // Used to synchronize writing to the errors file.
        private static SemaphoreSlim _errorsSemaphore = new SemaphoreSlim(1, 1);
        // FileStream object used when writing to the errors file.
        private FileStream _errorsFileStream;
        // Streamwriter object used when writing to the errors file.
        private StreamWriter _errorsStreamWriter;
        // The path to the errors file
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

        /// <summary>
        /// Private constructor to support Singleton pattern.
        /// </summary>
        private LogWriter() {            
            InitializeLog();
            InitializeErrorLog();
        }

        /// <summary>
        /// Creates the directory structure and new log file, if required.
        /// </summary>
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

        /// <summary>
        /// Creates the directory structure and an errors file.
        /// </summary>
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

        /// <summary>
        /// Writes to the log file, creating a new one if necessary.
        /// </summary>
        /// <param name="message"></param>
        public void Write(string message)
        {
            try {
                // Prevent contention for the file.
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

                // Release the lock for the next write operation.
                _semaphore.Release();
            } catch(Exception ex) {
                // Release the lock
                _semaphore.Release();
                LogErrors(ex);
            }
        }

        public void LogErrors(Exception ex) {
            try {
                // Prevent contention for the errors file.
                _errorsSemaphore.Wait();
                
                // Creates a new file if it doesn't yet exist, or gets a reference to the existing file.
                if(_errorsFileStream == null || _errorsStreamWriter == null) {
                    InitializeErrorLog();
                }
                                
                // Add the error to the end of the file.
                _errorsFileStream.Seek(0, SeekOrigin.End);
                _errorsStreamWriter.WriteLine($"ERROR - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} - {ex.GetBaseException()}");
                _errorsStreamWriter.Flush();

                // Release the lock
                _errorsSemaphore.Release();
            } catch(Exception) {
                // Release the lock
                _errorsSemaphore.Release();
            }             
        }
    }
}
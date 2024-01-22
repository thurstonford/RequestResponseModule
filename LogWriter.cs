using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RequestResponseModule
{
    public sealed class LogWriter
    {
        // Lazy load the instance so we don't have to worry about
        // double lock checking when creating the single instance.
        private static readonly Lazy<LogWriter> _instance = new Lazy<LogWriter>(() => new LogWriter());

        private static readonly string _path = System.IO.Path.Combine(AppContext.BaseDirectory, "Logs/RequestResponse/");
        private static readonly string _dateFormat = "yyyy-MM-dd HH:mm:ss.fff";

        // Used to synchronize writing to the log file.
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);        
        // FileStream object used when writing to the log file.
        private static FileStream _fileStream;
        // Streamwriter object used when writing to the log file.
        private static StreamWriter _streamWriter;
        // Used to synchronize writing to the errors file.
        private SemaphoreSlim _errorsSemaphore = new SemaphoreSlim(1, 1);
        // FileStream object used when writing to the errors file.
        private static FileStream _errorsFileStream;
        // Streamwriter object used when writing to the errors file.
        private static StreamWriter _errorsStreamWriter;

        public static LogWriter Instance {
            get {
                return _instance.Value;
            }
        }

        private string LogFilePath { 
            get { 
                return $"{Path}/{DateTime.Now:yyyyMMdd}.json"; 
            } 
        }

        private string ErrorFilePath {
            get {
                return $"{Path}/Errors.log";
            }
        }

        private string Path { 
            get { return _path; } 
        }

        private SemaphoreSlim LogSemaphore { 
            get { return Instance._semaphore; }
        }

        private static FileStream LogFileStream { 
            get { return _fileStream; }
            set { _fileStream = value; }
        }

        private static StreamWriter LogStreamWriter { 
            get { return _streamWriter; }
            set { _streamWriter = value; }
        }

        private SemaphoreSlim ErrorsSemaphore { 
            get { return Instance._errorsSemaphore; } 
        }

        private static FileStream ErrorsFileStream {
            get { return _errorsFileStream; }
            set { _errorsFileStream = value; }
        }

        private static StreamWriter ErrorsStreamWriter {
            get { return _errorsStreamWriter; }
            set { _errorsStreamWriter = value; }
        }

        /// <summary>
        /// Private constructor to support Singleton pattern.
        /// </summary>
        private LogWriter() {
            CreateDirectoryStructure();
            InitializeLog();            
            InitializeErrorLog();
        }

        private void CreateDirectoryStructure() {
            try {
                if(!Directory.Exists(Path)) {
                    Directory.CreateDirectory(Path);
                }
            } catch(Exception ex) {
                LogErrors(ex);
            }
        }

        /// <summary>
        /// Creates the directory structure and new log file, if required.
        /// </summary>
        private void InitializeLog() {
            try {
                LogFileStream = new FileStream(LogFilePath, FileMode.OpenOrCreate);
                LogStreamWriter = new StreamWriter(LogFileStream);                
            } catch(Exception ex) {
                LogErrors(ex);
            } 
        }        

        /// <summary>
        /// Creates the directory structure and an errors file.
        /// </summary>
        private void InitializeErrorLog() {
            try {
                ErrorsFileStream = new FileStream(ErrorFilePath, FileMode.OpenOrCreate);
                ErrorsStreamWriter = new StreamWriter(ErrorsFileStream);                
            } catch(Exception ex) {
                LogErrors(ex);
            }
        }

        /// <summary>
        /// Writes to the log file, creating a new one if necessary.
        /// </summary>
        /// <param name="message"></param>
        public void Write(string message) {
            try {                
                // Prevent contention for the file.
                LogSemaphore.Wait();

                // We may have rolled over into a new day so create a
                // new log file and dereference the previous log file.
                if(!File.Exists(LogFilePath)) {
                    try {
                        LogStreamWriter?.Close();
                        LogFileStream?.Close();
                    } catch(Exception ex) {
                        LogErrors(ex);
                    }
                }

                if(LogFileStream == null || LogStreamWriter == null) {
                    InitializeLog();
                }

                //write to the last line                
                LogFileStream.Seek(0, SeekOrigin.End);
                LogStreamWriter.WriteLine(message);                
                //LogStreamWriter.WriteLine(message);
                LogStreamWriter.Flush();

                // Release the lock for the next write operation.
                LogSemaphore.Release();
            } catch(Exception ex) {
                // Release the lock
                LogSemaphore.Release();
                LogErrors(ex);
            }
        }

        private void LogErrors(Exception ex) {
            try {
                // Prevent contention for the errors file.
                ErrorsSemaphore.Wait();
                
                // Creates a new file if it doesn't yet exist, or gets a reference to the existing file.
                if(ErrorsFileStream == null || ErrorsStreamWriter == null) {
                    InitializeErrorLog();
                }

                // Add the error to the end of the file.
                ErrorsFileStream.Seek(0, SeekOrigin.End);
                ErrorsStreamWriter.WriteLine($"ERROR - {DateTime.Now.ToString(_dateFormat)} - {ex.GetBaseException()}");
                ErrorsStreamWriter.Flush();

                // Release the lock
                ErrorsSemaphore.Release();
            } catch(Exception) {
                // Release the lock
                ErrorsSemaphore.Release();
            }             
        }
    }
}
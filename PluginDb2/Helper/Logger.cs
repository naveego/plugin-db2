using System;
using System.IO;
using System.Threading;
using Grpc.Core;
using Aunalytics.Sdk.Plugins;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace PluginDb2.Helper
{
    public static class Logger
    {
        private static string _logPrefix = "";
        private static string _fileName = @"plugin-db2-log.txt";
        private static LogLevel _level = LogLevel.Info;

        /// <summary>
        /// Initializes the logger
        /// </summary>
        public static void Init(string logPath = "logs")
        {
            // remove any existing loggers
            CloseAndFlush();
            
            // ensure log directory exists
            Directory.CreateDirectory(logPath);
            
            // setup serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Async(
                    sinkConfig =>
                    {
                        sinkConfig.File(
                            $"logs/{_fileName}",
                            rollingInterval: RollingInterval.Day,
                            shared: true,
                            rollOnFileSizeLimit: true
                        );
                        sinkConfig.StdErrSink();
                    })
                .CreateLogger();
        }

        /// <summary>
        /// Closes the logger and flushes any pending messages in the buffer
        /// </summary>
        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }
        
        /// <summary>
        /// Deletes log file if it is older than 7 days
        /// </summary>
        public static void Clean()
        {
            if (File.Exists(_fileName))
            {
                if ((File.GetCreationTime(_fileName) - DateTime.Now).TotalDays > 7)
                {
                    File.Delete(_fileName);
                }
            }
        }

        /// <summary>
        /// Logging method for Verbose messages
        /// </summary>
        /// <param name="message"></param>
        public static void Verbose(string message)
        {
            if (_level < LogLevel.Trace)
            {
                return;
            }
            
            GrpcEnvironment.Logger.Debug(message);
            
            Log.Verbose($"{_logPrefix} {message}");
        }
        
        /// <summary>
        /// Logging method for Debug messages
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            if (_level < LogLevel.Debug)
            {
                return;
            }
            
            GrpcEnvironment.Logger.Debug(message);
            
            Log.Debug($"{_logPrefix} {message}");
        }
        /// <summary>
        /// Logging method for Info messages
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            if (_level < LogLevel.Info)
            {
                return;
            }
            
            GrpcEnvironment.Logger.Info(message);
            
            Log.Information($"{_logPrefix} {message}");
        }
        
        /// <summary>
        /// Logging method for Error messages
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        public static void Error(Exception exception, string message)
        {
            if (_level < LogLevel.Error)
            {
                return;
            }
            
            GrpcEnvironment.Logger.Error(exception, message);
            
            Log.Error(exception, $"{_logPrefix} {message}");
        }
        
        /// <summary>
        /// Logging method for Error messages to the context
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        public static void Error(Exception exception, string message, ServerCallContext context)
        {
            if (_level < LogLevel.Error)
            {
                return;
            }
            
            GrpcEnvironment.Logger.Error(exception, message);
            context.Status = new Status(StatusCode.Unknown, message);
            
            Log.Error(exception, $"{_logPrefix} {message}");
        }

        /// <summary>
        /// Sets the log level 
        /// </summary>
        /// <param name="level"></param>
        public static void SetLogLevel(LogLevel level)
        {
            _level = level;
        }

        /// <summary>
        /// Sets a 
        /// </summary>
        /// <param name="logPrefix"></param>
        public static void SetLogPrefix(string logPrefix)
        {
            _logPrefix = $"<{logPrefix}>";
        }
    }
    
    public class StdErrSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public StdErrSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
        }
        
        public void Emit(LogEvent logEvent)
        {
            var level = GetLevelString(logEvent.Level);
            Console.Error.WriteLine($"[{level}] {logEvent.RenderMessage(_formatProvider)}");
        }

        private string GetLevelString(LogEventLevel level) => level switch
        {
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "ERROR",
            LogEventLevel.Verbose => "TRACE",
            LogEventLevel.Warning => "WARN",
            _ => "INFO"
        };

    }

    public static class StdErrSinkExtensions
    {
        public static LoggerConfiguration StdErrSink(this LoggerSinkConfiguration loggerConfiguration,
            IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new StdErrSink(formatProvider));
        }
    }
}
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AutoHPMA.Helpers
{
    public class LogFileSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        private readonly string _logDirectory;
        private string _currentLogFile;

        public LogFileSink(IFormatProvider formatProvider = null)
        {
            _formatProvider = formatProvider;
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            EnsureLogDirectoryExists();
            _currentLogFile = GenerateLogFileName();
        }

        private void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private string GenerateLogFileName()
        {
            var now = DateTime.Now;
            var timestamp = now.ToString("MM-dd-HHmm");
            var hash = GenerateShortHash(now.Ticks.ToString());
            return Path.Combine(_logDirectory, $"{timestamp}--{hash}.log");
        }

        private string GenerateShortHash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8);
            }
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                var message = logEvent.RenderMessage(_formatProvider);
                var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var level = logEvent.Level.ToString().ToUpper();
                var logEntry = $"[{timestamp}] [{level}] {message}";

                if (logEvent.Exception != null)
                {
                    logEntry += $"\n{logEvent.Exception}";
                }

                File.AppendAllText(_currentLogFile, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志文件失败: {ex.Message}");
            }
        }
    }
} 
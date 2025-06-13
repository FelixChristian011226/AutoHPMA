using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using AutoHPMA.Config;

namespace AutoHPMA.Helpers.LogHelper
{
    public class LogFileSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        private readonly string _logDirectory;
        private string _currentLogFile;
        private readonly AppSettings _settings;

        public LogFileSink(IFormatProvider formatProvider = null)
        {
            _formatProvider = formatProvider;
            _settings = AppSettings.Load();
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            EnsureLogDirectoryExists();
            _currentLogFile = GenerateLogFileName();
            CleanupOldLogs();
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

        private void CleanupOldLogs()
        {
            if (_settings.LogFileLimit <= 0) return;

            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (logFiles.Count > _settings.LogFileLimit)
                {
                    var filesToDelete = logFiles.Skip(_settings.LogFileLimit-1);
                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"删除日志文件失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理日志文件时发生错误: {ex.Message}");
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
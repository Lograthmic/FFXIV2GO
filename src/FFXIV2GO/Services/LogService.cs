using System.IO;
using System.Text;

namespace FFXIV2GO.Services;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

/// <summary>
/// 简易文件日志：写入部署根 logs\ffxiv2go.log，线程安全，写入失败不阻断应用。
/// 日志级别：Debug(0) &lt; Info(1) &lt; Warn(2) &lt; Error(3)，低于当前级别的消息不写入。
/// </summary>
public static class LogService
{
    private static readonly object Sync = new();
    private static string? _filePath;

    public static LogLevel Level { get; set; } = LogLevel.Info;

    public static void ApplyLevel(string? level)
    {
        Level = AppConfig.NormalizeLogLevel(level) switch
        {
            "Debug" => LogLevel.Debug,
            "Info" => LogLevel.Info,
            "Warn" => LogLevel.Warn,
            "Error" => LogLevel.Error,
            _ => LogLevel.Info
        };
    }

    public static string LogFilePath
    {
        get
        {
            if (_filePath is null)
            {
                var dir = Path.Combine(DeploymentRoot.Path, "logs");
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch
                {
                    // 无法创建则退回临时目录
                    dir = Path.GetTempPath();
                }

                _filePath = Path.Combine(dir, "ffxiv2go.log");
            }

            return _filePath;
        }
    }

    public static void Debug(string message) => Write(LogLevel.Debug, message);
    public static void Info(string message) => Write(LogLevel.Info, message);
    public static void Warn(string message) => Write(LogLevel.Warn, message);
    public static void Error(string message) => Write(LogLevel.Error, message);

    private static void Write(LogLevel level, string message)
    {
        if (level < Level)
        {
            return;
        }

        try
        {
            lock (Sync)
            {
                File.AppendAllText(LogFilePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level,-5}] {message}{Environment.NewLine}",
                    Encoding.UTF8);
            }
        }
        catch
        {
            // 日志写入失败不阻断应用
        }
    }
}

namespace VoxelGame;

public static class Debug
{
    public enum LogType
    {
        Message,
        Warning,
        Error,
        Critical
    }
    
    public static string ToCustomString(this LogType lt)
    {
        switch (lt)
        {
            case LogType.Message:
                return "MESSAGE";
            case LogType.Warning:
                return "WARNING";
            case LogType.Error:
                return "ERROR";
            case LogType.Critical:
                return "CRITICAL";
            default:
                throw new ArgumentOutOfRangeException(nameof(lt), lt, null);
        }
    }
    
    private static List<LogMessage> _logMessages;

    static Debug()
    {
        _logMessages = new();
    }
    
    public static void Log(object content)
    {
        var log = new LogMessage(DateTime.Now, content.ToString() ?? "No message", LogType.Message);
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(log.ToString());
        Console.ResetColor();
        
        _logMessages.Add(log);
    }
    
    public static void LogWarning(object content)
    {
        var log = new LogMessage(DateTime.Now, content.ToString() ?? "No warning", LogType.Warning);
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(log.ToString());
        Console.ResetColor();
        
        _logMessages.Add(log);
    }
    
    public static void LogError(object content)
    {
        var log = new LogMessage(DateTime.Now, content.ToString() ?? "No error", LogType.Error);
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(log.ToString());
        Console.ResetColor();
        
        _logMessages.Add(log);
    }
    
    public static void LogException(Exception e, bool includeStackTrace = false)
    {
        var log = new LogMessage(DateTime.Now, includeStackTrace ? $"{e.Message}\nStack trance:\n{e.StackTrace}" : e.Message, LogType.Critical);
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(log.ToString());
        Console.ResetColor();
        
        _logMessages.Add(log);
    }
    
    private struct LogMessage(DateTime time, string content, LogType type)
    {
        public string Time = time.ToString("hh:mm:ss");
        public string Content = content;

        public LogType Type = type;

        public override string ToString()
        {
            return $"[{Time}] {Type.ToCustomString()} - {Content}";
        }
    }
}
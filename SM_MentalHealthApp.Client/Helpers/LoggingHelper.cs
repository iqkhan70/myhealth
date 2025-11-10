namespace SM_MentalHealthApp.Client.Helpers;

/// <summary>
/// Helper class for client-side logging
/// Uses conditional compilation to remove debug logs in release builds
/// </summary>
public static class LoggingHelper
{
#if DEBUG
    public static void LogDebug(string message)
    {
        Console.WriteLine($"[DEBUG] {message}");
    }
    
    public static void LogInfo(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }
    
    public static void LogWarning(string message)
    {
        Console.WriteLine($"[WARN] {message}");
    }
    
    public static void LogError(string message, Exception? ex = null)
    {
        Console.WriteLine($"[ERROR] {message}");
        if (ex != null)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }
#else
    // In release builds, these methods do nothing
    public static void LogDebug(string message) { }
    public static void LogInfo(string message) { }
    public static void LogWarning(string message) { }
    public static void LogError(string message, Exception? ex = null) { }
#endif
}


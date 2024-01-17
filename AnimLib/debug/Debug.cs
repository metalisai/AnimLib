using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace AnimLib;

/// <summary>
/// Debugging utilities. When using these in animations, note that these a run during bake time, so they are not printed while seeking in the editor application.
/// </summary>
static public class Debug {
    internal static ConcurrentDictionary<string, DateTime> lastLog = new ();

    private static bool shouldSkip(float rate, string identifier) {
        float msgWaitTime = 1.0f / rate;
        var now = DateTime.Now;
        if(lastLog.TryGetValue(identifier, out var last)) {
            var diff = now - last;
            if(diff.TotalSeconds < msgWaitTime) {
                return true;
            }
        }
        lastLog[identifier] = now;
        return false;
    }

    /// <summary>
    /// Log a message.
    /// </summary>
    public static void Log(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0, float? rate = null) {
#if DEBUG
        var identifier = callerfile + lineNumber;
        if(rate != null && shouldSkip(rate.Value, identifier)) {
            return;
        }
        var file = Path.GetFileName(callerfile);
        Console.WriteLine($"{file}:{lineNumber} {t}");
#endif
    }

    /// <summary>
    /// Log a warning.
    /// </summary>
    public static void Warning(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0, float? rate = null) {
#if DEBUG
        var identifier = callerfile + lineNumber;
        if(rate != null && shouldSkip(rate.Value, identifier)) {
            return;
        }
        var file = Path.GetFileName(callerfile);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{file}:{lineNumber} {t}");
        Console.ResetColor();
#endif
    }

    /// <summary>
    /// Log an error.
    /// </summary>
    public static void Error(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0, float? rate = null) {
#if DEBUG
        var identifier = callerfile + lineNumber;
        if(rate != null && shouldSkip(rate.Value, identifier)) {
            return;
        }
        var file = Path.GetFileName(callerfile);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{file}:{lineNumber} {t}");
        Console.ResetColor();
        Console.WriteLine($"Trace: {System.Environment.StackTrace}");
#endif
    }

    /// <summary>
    /// Development time debugging log (colored because it is intended to be removed).
    /// </summary>
    public static void TLog(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0, float? rate = null) {
#if DEBUG
        var identifier = callerfile + lineNumber;
        if(rate != null && shouldSkip(rate.Value, identifier)) {
            return;
        }
        var file = Path.GetFileName(callerfile);
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"{file}:{lineNumber} {t}");
        Console.ResetColor();
#endif
    }

    /// <summary>
    /// Development time debugging log (colored because it is intended to be removed).
    /// </summary>
    public static void TLogWithTrace(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0, float? rate = null) {
#if DEBUG
        var identifier = callerfile + lineNumber;
        if(rate != null && shouldSkip(rate.Value, identifier)) {
            return;
        }
        var file = Path.GetFileName(callerfile);
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"{file}:{lineNumber} {t}");
        Console.WriteLine($"{Environment.StackTrace}");
        Console.ResetColor();
#endif
    }

    /// <summary>
    /// Asserts that the given condition is true. If not, throws an exception.
    /// </summary>
    public static void Assert(bool condition, string t = "Assertion failed", [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0) {
#if DEBUG
        if (!condition) {
            var file = Path.GetFileName(callerfile);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{file}:{lineNumber} {t}");
            Console.ResetColor();
            Console.WriteLine($"Trace: {System.Environment.StackTrace}");
            throw new Exception(t);
        }
#endif
    }
}

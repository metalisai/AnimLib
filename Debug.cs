using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace AnimLib {
    static internal class Debug {

        internal static void Log(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0) {
#if DEBUG
            var file = Path.GetFileName(callerfile);
            Console.WriteLine($"{file}:{lineNumber} {t}");
#endif
        }

        internal static void Warning(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0) {
#if DEBUG
            var file = Path.GetFileName(callerfile);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{file}:{lineNumber} {t}");
            Console.ResetColor();
#endif
        }

        internal static void Error(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0) {
#if DEBUG
            var file = Path.GetFileName(callerfile);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{file}:{lineNumber} {t}");
            Console.ResetColor();
#endif
        }

        // development time debugging log (colored because it is intended to be removed)
        internal static void TLog(string t, [CallerFilePath] string callerfile = "", [CallerLineNumber] int lineNumber = 0) {
#if DEBUG
            var file = Path.GetFileName(callerfile);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{file}:{lineNumber} {t}");
            Console.ResetColor();
#endif
        }


    }
}

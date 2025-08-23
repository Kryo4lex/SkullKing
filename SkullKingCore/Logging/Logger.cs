using System.Reflection;

namespace SkullKingCore.Logging
{
    public sealed class Logger
    {
        private static readonly object _lock = new object();
        private static readonly Lazy<Logger> _lazyInstance = new Lazy<Logger>(() => new Logger());
        private string _logFilePath;
        public const string LongDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        // Global flag to enable or disable logging
        private static bool _isLoggingEnabled = true;
        public static bool IsLoggingEnabled => _isLoggingEnabled;

        // Singleton instance for global logging.
        public static Logger Instance
        {
            get { return _lazyInstance.Value; }
        }

        // Private constructor to prevent direct instantiation.
        private Logger()
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            string assemblyNameToUse;

            if (entryAssembly != null)
            {
                assemblyNameToUse = entryAssembly.GetName().Name!;
            }
            else
            {
                assemblyNameToUse = $"{nameof(SkullKing)}";
            }

            _logFilePath = $"{assemblyNameToUse}_log.txt";
        }

        // Initialize with custom log file path.
        public void Initialize(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public static void EnableLogging() => _isLoggingEnabled = true;
        public static void DisableLogging() => _isLoggingEnabled = false;
        public static void ToggleLogging() => _isLoggingEnabled = !_isLoggingEnabled;

        // Thread-safe logging method.
        public void Log(string message, bool force = false)
        {
            if (!IsLoggingEnabled && !force) return;

            lock (_lock)
            {
                using (StreamWriter writer = new StreamWriter(_logFilePath, true))
                {
                    writer.WriteLine("[" + DateTime.Now.ToString(LongDateTimeFormat) + "]" + $":{message}");
                }
            }
        }

        public void WriteToConsoleAndLog(string message, bool force = false)
        {
            Console.WriteLine(message);
            Log(message, force );
        }

        // Overload to log exception details.
        public void Log(Exception ex, bool force = false)
        {
            if (!IsLoggingEnabled) return;

            Log($"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}", force);
        }
    }
}
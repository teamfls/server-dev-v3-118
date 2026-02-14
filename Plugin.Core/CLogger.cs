using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Plugin.Core.Colorful;
using Plugin.Core.Enums;
using Plugin.Core.Utility;
using Console = Plugin.Core.Colorful.Console;

namespace Plugin.Core
{
    public static class CLogger
    {
        private static readonly string Date = DateTimeUtil.Now("yyyy-MM-dd--HH-mm-ss");
        private static readonly string CommandPath = $"Logs/{LoggerType.Command}";
        private static readonly string ConsolePath = $"Logs/{LoggerType.Console}";
        private static readonly string DebugPath = $"Logs/{LoggerType.Debug}";
        private static readonly string ErrorPath = $"Logs/{LoggerType.Error}";
        private static readonly string HackPath = $"Logs/{LoggerType.Hack}";
        private static readonly string OpcodePath = $"Logs/{LoggerType.Opcode}";
        private static readonly object Sync = new object();

        public static DateTime LastAuthException = DateTime.MinValue;
        public static DateTime LatchAuthSession = DateTime.MinValue;
        public static DateTime LastGameException = DateTime.MinValue;
        public static DateTime LastGameSession = DateTime.MinValue;
        public static DateTime LastMatchSocket = DateTime.MinValue;
        public static DateTime LastMatchBuffer = DateTime.MinValue;

        static CLogger()
        {
            try
            {
                string[] Directorys = new string[] { "Logs/", CommandPath, ConsolePath, DebugPath, ErrorPath, HackPath, OpcodePath };
                foreach (string Local in Directorys)
                {
                    if (!Directory.Exists(Local))
                    {
                        Directory.CreateDirectory(Local);
                    }
                }

                //System.Console.WriteLine($"[CLogger] Initialized at {DateTimeUtil.Now()}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[CLogger Init Error] {ex.Message}");
                try
                {
                    Print(ex.Message, LoggerType.Error, ex);
                }
                catch
                {
                    System.Console.WriteLine($"[CRITICAL] Logger initialization failed: {ex}");
                }
            }
        }

        #region Getter/Setter Methods
        public static DateTime GetLastGameException() => LastGameException;
        public static DateTime SetLastGameException(DateTime dateTime) => LastGameException = dateTime;
        public static DateTime GetLastAuthException() => LastAuthException;
        public static DateTime SetLastAuthException(DateTime dateTime) => LastAuthException = dateTime;
        public static DateTime GetLastGameSession() => LastGameSession;
        public static DateTime SetLastGameSession(DateTime dateTime) => LastGameSession = dateTime;
        public static DateTime GetLastMatchSocket() => LastMatchSocket;
        public static DateTime SetLastMatchSocket(DateTime dateTime) => LastMatchSocket = dateTime;
        public static DateTime GetLastMatchBuffer() => LastMatchBuffer;
        public static DateTime SetLastMatchBuffer(DateTime dateTime) => LastMatchBuffer = dateTime;
        public static DateTime GetLatchAuthSession() => LatchAuthSession;
        public static DateTime SetLatchAuthSession(DateTime dateTime) => LatchAuthSession = dateTime;

        public static DateTime UpdateLastMatchSocket() => LastMatchSocket = DateTime.Now;
        public static DateTime UpdateLastMatchBuffer() => LastMatchBuffer = DateTime.Now;
        #endregion

        public static void Print(string Text, LoggerType Type, Exception Ex = null)
        {
            try
            {
                if (Type == LoggerType.Error && Ex != null)
                {
                    LastGameException = DateTimeUtil.Now();
                }

                switch (Type)
                {
                    case LoggerType.Info:
                        Execute("{0}", Text, Ex, Type);
                        break;
                    case LoggerType.Warning:
                        Execute("{1}", Text, Ex, Type);
                        break;
                    case LoggerType.Debug:
                        if (ConfigLoader.DebugMode)
                        {
                            Execute("{2}", Text, Ex, Type);
                        }
                        break;
                    case LoggerType.Error:
                        Execute("{3}", Text, Ex, Type);
                        break;
                    case LoggerType.Hack:
                        Execute("{4}", Text, Ex, Type);
                        break;
                    case LoggerType.Command:
                        Execute("{5}", Text, Ex, Type);
                        break;
                    case LoggerType.Console:
                        Execute("{5}", Text, Ex, Type);
                        break;
                    case LoggerType.Opcode:
                        Execute("{6}", Text, Ex, Type);
                        break;
                    default:
                        System.Console.WriteLine($"[UNKNOWN] {Text}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[CLogger Error] {ex.Message}");
                System.Console.WriteLine($"[Original Message] {Text}");
            }
        }

        private static string[] StackTraces(Exception Ex)
        {
            string[] Traces = new string[3] { "", "", "" };
            try
            {
                if (Ex != null)
                {
                    StackTrace Trace = new StackTrace(Ex, true);
                    if (Trace != null && Trace.FrameCount > 0)
                    {
                        var frame = Trace.GetFrame(0);
                        if (frame != null)
                        {
                            Traces[0] = frame.GetMethod()?.ReflectedType?.Name ?? "Unknown";
                            Traces[1] = frame.GetFileLineNumber().ToString();
                            Traces[2] = frame.GetFileColumnNumber().ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[StackTrace Error] {ex.Message}");
            }
            return Traces;
        }

        private static void Execute(string Code, string Text, Exception Ex, LoggerType PathGroup)
        {
            try
            {
                lock (Sync)
                {
                    try
                    {
                        WriteToConsole(Code, Text, PathGroup, Ex);
                    }
                    catch (Exception colorEx)
                    {
                        string timestamp = DateTimeUtil.Now("HH:mm:ss");
                        string typePrefix = GetTypePrefix(PathGroup);
                        System.Console.WriteLine($"[COLOR CONSOLE FAILED] {colorEx.Message}");
                        System.Console.WriteLine($"{timestamp} {typePrefix} {Text}");
                    }

                    try
                    {
                        string FinalPath = GetLogPath(PathGroup, Ex);
                        LOG(Text, Ex, FinalPath);
                    }
                    catch (Exception logEx)
                    {
                        System.Console.WriteLine($"[FILE LOG FAILED] {logEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Execute CRITICAL ERROR] {ex.Message}");
                System.Console.WriteLine($"[STACK TRACE] {ex.StackTrace}");
                System.Console.WriteLine($"[ORIGINAL MESSAGE] {Text}");
            }
        }

        private static void WriteToConsole(string Code, string Text, LoggerType PathGroup, Exception Ex)
        {
            try
            {
                string timestamp = DateTimeUtil.Now("HH:mm:ss");
                string separator = "|";
                string prefix = GetColoredPrefix(PathGroup);

                if (PathGroup == LoggerType.Error && Ex != null)
                {
                    try
                    {
                        Console.Write($"[{timestamp}] ", ColorUtil.Silver);
                        Console.Write($"{prefix} ", GetPrefixColor(PathGroup));
                        Console.Write($"{separator} ", ColorUtil.Silver);
                        Console.WriteLine(Text, ColorUtil.Red);

                        if (Ex != null)
                        {
                            var traces = StackTraces(Ex);
                            Console.Write("         +- ", ColorUtil.Silver);
                            Console.Write($"Error Type: ", ColorUtil.Yellow);
                            Console.WriteLine($"{Ex.GetType().Name}", ColorUtil.Red);

                            Console.Write("         +- ", ColorUtil.Silver);
                            Console.Write($"Location: ", ColorUtil.Yellow);
                            Console.WriteLine($"{traces[0]} (Line {traces[1]})", ColorUtil.Fuchsia);

                            if (!string.IsNullOrEmpty(Ex.Message) && Ex.Message != Text)
                            {
                                Console.Write("         +- ", ColorUtil.Silver);
                                Console.Write($"Message: ", ColorUtil.Yellow);
                                Console.WriteLine($"{Ex.Message}", ColorUtil.Red);
                            }
                        }
                    }
                    catch
                    {
                        System.Console.WriteLine($"[{timestamp}] {prefix} {separator} {Text}");
                        if (Ex != null) System.Console.WriteLine($"         +- {Ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        Console.Write($"[{timestamp}] ", ColorUtil.Silver);
                        Console.Write($"{prefix} ", GetPrefixColor(PathGroup));
                        Console.Write($"{separator} ", ColorUtil.Silver);
                        Console.WriteLine(Text, GetTextColor(PathGroup));
                    }
                    catch
                    {
                        System.Console.WriteLine($"[{timestamp}] {prefix} {separator} {Text}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Console Write Error] {ex.Message}");
                System.Console.WriteLine($"[Original] {Text}");
            }
        }

        private static string GetColoredPrefix(LoggerType type)
        {
            switch (type)
            {
                case LoggerType.Info:
                    return "INFO ";
                case LoggerType.Warning:
                    return "WARN ";
                case LoggerType.Debug:
                    return "DEBUG";
                case LoggerType.Error:
                    return "ERROR";
                case LoggerType.Hack:
                    return "HACK ";
                case LoggerType.Command:
                    return "CMD  ";
                case LoggerType.Console:
                    return "CONS ";
                case LoggerType.Opcode:
                    return "OP   ";
                default:
                    return "?????";
            }
        }

        private static System.Drawing.Color GetPrefixColor(LoggerType type)
        {
            switch (type)
            {
                case LoggerType.Info:
                    return ColorUtil.Green;
                case LoggerType.Warning:
                    return ColorUtil.Yellow;
                case LoggerType.Debug:
                    return ColorUtil.Cyan;
                case LoggerType.Error:
                    return ColorUtil.Red;
                case LoggerType.Hack:
                    return ColorUtil.Fuchsia;
                case LoggerType.Command:
                    return ColorUtil.Blue;
                case LoggerType.Console:
                    return ColorUtil.Green;
                case LoggerType.Opcode:
                    return ColorUtil.Cyan;
                default:
                    return ColorUtil.White;
            }
        }

        private static System.Drawing.Color GetTextColor(LoggerType type)
        {
            switch (type)
            {
                case LoggerType.Info:
                    return ColorUtil.White;
                case LoggerType.Warning:
                    return ColorUtil.Yellow;
                case LoggerType.Debug:
                    return ColorUtil.LightGrey;
                case LoggerType.Error:
                    return ColorUtil.Red;
                case LoggerType.Hack:
                    return ColorUtil.Red;
                case LoggerType.Command:
                    return ColorUtil.Green;
                case LoggerType.Console:
                    return ColorUtil.White;
                case LoggerType.Opcode:
                    return ColorUtil.Cyan;
                default:
                    return ColorUtil.LightGrey;
            }
        }

        private static string GetTypePrefix(LoggerType type)
        {
            switch (type)
            {
                case LoggerType.Info:
                    return "[INFO]";
                case LoggerType.Warning:
                    return "[WARN]";
                case LoggerType.Debug:
                    return "[DEBUG]";
                case LoggerType.Error:
                    return "[ERROR]";
                case LoggerType.Hack:
                    return "[HACK]";
                case LoggerType.Command:
                    return "[CMD]";
                case LoggerType.Console:
                    return "[CONS]";
                case LoggerType.Opcode:
                    return "[OP]";
                default:
                    return "[?]";
            }
        }

        private static string GetLogPath(LoggerType PathGroup, Exception Ex)
        {
            if (PathGroup.Equals(LoggerType.Info) || PathGroup.Equals(LoggerType.Warning))
            {
                return $"Logs/{Date}.log";
            }
            else if (PathGroup.Equals(LoggerType.Error))
            {
                string errorClass = Ex != null ? StackTraces(Ex)[0] : "NULL";
                return $"Logs/{PathGroup}/{Date}-{errorClass}.log";
            }
            else
            {
                return $"Logs/{PathGroup}/{Date}.log";
            }
        }

        private static void LOG(string Text, Exception Ex, string FinalPath)
        {
            try
            {
                string directory = Path.GetDirectoryName(FinalPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (FileStream File = new FileStream(FinalPath, FileMode.Append))
                using (StreamWriter Stream = new StreamWriter(File, Encoding.UTF8))
                {
                    string TextFinal = Ex != null ? $"{Text} \n{Ex}" : Text;
                    Stream.WriteLine($"{DateTimeUtil.Now("yyyy-MM-dd HH:mm:ss")} {TextFinal}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[File Log Error] {ex.Message}");
            }
        }

        public static void TestLogging()
        {
            Print("Server initialization complete", LoggerType.Info);
            Print("Configuration file not found, using defaults", LoggerType.Warning);
            Print("Loading player data from database...", LoggerType.Debug);

            try
            {
                throw new InvalidOperationException("Sample error for testing");
            }
            catch (Exception ex)
            {
                Print("Failed to load player statistics", LoggerType.Error, ex);
            }

            Print("Suspicious activity detected from IP 192.168.1.100", LoggerType.Hack);
            Print("/kick player123 executed by admin", LoggerType.Command);
            Print("Player connected successfully", LoggerType.Console);
            Print("Received packet 0x4E21 from client", LoggerType.Opcode);
        }

        public static void TestSetters()
        {
            try
            {
                DateTime now = DateTime.Now;
                SetLastAuthException(now);
                SetLastGameException(now);
                SetLastGameSession(now);
                SetLastMatchSocket(now);
                SetLastMatchBuffer(now);
                SetLatchAuthSession(now);

                System.Console.WriteLine("[SUCCESS] All setter methods working correctly");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERROR] Setter test failed: {ex.Message}");
            }
        }

        public static void PrintSuccess(string message)
        {
            try
            {
                Console.Write("[+] ", ColorUtil.Green);
                Console.WriteLine(message, ColorUtil.Green);
            }
            catch
            {
                System.Console.WriteLine($"[+] {message}");
            }
        }

        public static void PrintFailed(string message)
        {
            try
            {
                Console.Write("[-] ", ColorUtil.Red);
                Console.WriteLine(message, ColorUtil.Red);
            }
            catch
            {
                System.Console.WriteLine($"[-] {message}");
            }
        }

        public static void PrintSection(string title)
        {
            try
            {
                Console.WriteLine();
                Console.Write("===== ", ColorUtil.Cyan);
                Console.Write(title, ColorUtil.White);
                Console.WriteLine(" =====", ColorUtil.Cyan);
            }
            catch
            {
                System.Console.WriteLine($"\n===== {title} =====");
            }
        }
    }
}
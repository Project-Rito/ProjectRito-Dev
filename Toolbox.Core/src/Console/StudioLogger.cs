using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolbox.Core
{
    /// <summary>
    /// A logger that keeps track of the processes in the studio program.
    /// This will keep track of progress handling for loading certain tasks.
    /// </summary>
    public class StudioLogger
    {
        private static List<Entry> Outputs = new List<Entry>();
        private static List<Entry> Errors = new List<Entry>();
        private static List<Entry> Warnings = new List<Entry>();

        private static string OutputLog;
        private static string ErrorLog;
        private static string WarningLog;

        private static int OutputLogLength;
        private static int ErrorLogLength;
        private static int WarningLogLength;

        private static string Line;

        /// <summary>
        /// Runs an event when the log information is updated.
        /// </summary>
        public static EventHandler OnLoggerUpdated;

        /// <summary>
        /// Runs an event when the progress has been updated with a precent value passed.
        /// Used to keep track of progress bar information.
        /// </summary>
        public static EventHandler OnProgressUpdated;

        public static string GetLog() {
            UpdateLog(Outputs, ref OutputLog, ref OutputLogLength);
            return OutputLog;
        }

        public static string GetErrorLog() {
            UpdateLog(Errors, ref ErrorLog, ref ErrorLogLength);
            return ErrorLog;
        }

        public static string GetWarningLog() {
            UpdateLog(Warnings, ref WarningLog, ref WarningLogLength);
            return WarningLog;
        }

        public static string GetLine() {
            return Line;
        }

        public static void ResetErrors()
        {
            Errors.Clear();
            Warnings.Clear();
            OnLoggerUpdated?.Invoke("", EventArgs.Empty);
        }

        static void UpdateLog(List<Entry> entries, ref string log, ref int logCount)
        {
            if (entries.Count > logCount)
            {
                for (int i = logCount; i < entries.Count; i++)
                {
                    log += entries[i].Text + "\n";
                    logCount++;
                }
            }
        }

        /// <summary>
        /// Writes a string of text to the logger.
        /// </summary>
        public static void WriteLine(string value, string category = "")
        {
            Outputs.Add(new Entry($"{value}", category));
            Update(value);
        }

        /// <summary>
        /// Writes a string of text to the logger using the error boolean.
        /// </summary>
        public static void WriteError(string value, string category = "")
        {
            Errors.Add(new Entry($"{value}", category));
            Update(value);
        }

        /// <summary>
        /// Writes a string of text to the logger using the error boolean.
        /// </summary>
        public static void WriteErrorException(string value, string category = "")
        {
            Errors.Add(new Entry($"{value}", category));
            Update(value);

            throw new Exception($"{value}");
        }

        /// <summary>
        /// Writes a string of text to the logger using the error boolean.
        /// </summary>
        public static void WriteWarning(string value, string category = "")
        {
            Warnings.Add(new Entry($"{value}", category));
            Update(value);
        }


        /// <summary>
        /// Passes a precent int value over to the progress handler.
        /// </summary>
        public static void UpdateProgress(int value) {
            OnProgressUpdated?.Invoke(value, EventArgs.Empty);
        }

        static void Update(string value) {
            Line = value;
            OnLoggerUpdated?.Invoke(value, EventArgs.Empty);
        }
    }

    class Entry
    {
        public string Text { get; set; }
        public string Category { get; set; }

        public Entry(string text, string category) {
            Text = text;
            Category = category;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapStudio.UI
{
    /// <summary>
    /// Keeps track of the progress of an operation.
    /// </summary>
    public class ProcessLoading
    {
        /// <summary>
        /// The current instance of the progress loader.
        /// </summary>
        public static ProcessLoading Instance = null;

        /// <summary>
        /// Checks if the progress is currently active.
        /// </summary>
        public bool IsLoading;

        /// <summary>
        /// The current amount of progress being set.
        /// </summary>
        public int ProcessAmount;

        /// <summary>
        /// The total amount of progress to target to.
        /// </summary>
        public int ProcessTotal;

        /// <summary>
        /// The process name to display.
        /// </summary>
        public string ProcessName;

        /// <summary>
        /// An event that updates when the progress has been altered.
        /// </summary>
        public EventHandler OnUpdated;

        public string Title = "";

        public ProcessLoading() {
            Instance = this;
        }

        public void UpdateIncrease(int amount, string process, string title = "Loading")
        {
            ProcessAmount += amount;
            ProcessName = process;
            OnUpdated?.Invoke(this, EventArgs.Empty);
            Title = title;
        }

        public void Update(int amount, int total, string process, string title = "Loading")
        {
            ProcessAmount = amount;
            ProcessTotal = total;
            ProcessName = process;
            OnUpdated?.Invoke(this, EventArgs.Empty);
            Title = title;
        }
    }
}

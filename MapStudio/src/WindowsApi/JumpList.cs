using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Shell;

namespace MapStudio.WindowsApi
{
    public class JumpListHelper
    {
        private JumpList list;

        /// <summary>
        /// Creating a JumpList for the application
        /// </summary>
        /// <param name="windowHandle"></param>
        public JumpListHelper()
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                list = JumpList.CreateJumpList();
                list.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent;
            }
        }

        /// <summary>
        /// Adds a list of all the recent projects in the tool.
        /// </summary>
        public void ReloadRecentList(List<string> recentProjects)
        {
            if (list == null)
                return;

            string exec = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            foreach (var project in recentProjects)
            {
                var name = new DirectoryInfo(project).Name;

                JumpListLink recentList = new JumpListLink(exec, name);
                recentList.Arguments = $"{project}/Project.json";
                list.AddUserTasks(recentList);
            }
            list.Refresh();
        }
    }
}

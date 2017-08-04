using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SolutionBuilder.View
{
    class UserPreferences
    {
        public double WindowTop { get; set; }
        public double WindowLeft { get; set; }
        public double WindowHeight { get; set; }
        public double WindowWidth { get; set; }
        public WindowState WindowState { get; set; }
        public UserPreferences()
        {
            //Load the settings
            Load();

            //Size it to fit the current screen
            SizeToFit();

            //Move the window at least partially into view
            MoveIntoView();
        }
        private void Load()
        {
            WindowTop = Properties.Settings.Default.WindowTop;
            WindowLeft = Properties.Settings.Default.WindowLeft;
            WindowHeight = Properties.Settings.Default.WindowHeight;
            WindowWidth = Properties.Settings.Default.WindowWidth;
            WindowState = Properties.Settings.Default.WindowState;
        }
        public void Save()
        {
            if (WindowState != System.Windows.WindowState.Minimized)
            {
                Properties.Settings.Default.WindowTop = WindowTop;
                Properties.Settings.Default.WindowLeft = WindowLeft;
                Properties.Settings.Default.WindowHeight = WindowHeight;
                Properties.Settings.Default.WindowWidth = WindowWidth;
                Properties.Settings.Default.WindowState = WindowState;

                Properties.Settings.Default.Save();
            }
        }
        public void SizeToFit()
        {
            if (WindowHeight > System.Windows.SystemParameters.VirtualScreenHeight)
            {
                WindowHeight = System.Windows.SystemParameters.VirtualScreenHeight;
            }

            if (WindowWidth > System.Windows.SystemParameters.VirtualScreenWidth)
            {
                WindowWidth = System.Windows.SystemParameters.VirtualScreenWidth;
            }
        }
        public void MoveIntoView()
        {
            if (WindowTop + WindowHeight / 2 >
                 System.Windows.SystemParameters.VirtualScreenHeight)
            {
                WindowTop =
                  System.Windows.SystemParameters.VirtualScreenHeight -
                  WindowHeight;
            }

            if (WindowLeft + WindowWidth / 2 >
                     System.Windows.SystemParameters.VirtualScreenWidth)
            {
                WindowLeft =
                  System.Windows.SystemParameters.VirtualScreenWidth -
                  WindowWidth;
            }

            if (WindowTop < 0)
            {
                WindowTop = 0;
            }

            if (WindowLeft < 0)
            {
                WindowLeft = 0;
            }
        }
    }
}

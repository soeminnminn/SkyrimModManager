using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace ModManager
{
    public static class RoutedCommands
    {
        public static RoutedCommand Backup = new RoutedCommand("Backup", typeof(Window));
        public static RoutedCommand Restore = new RoutedCommand("Restore", typeof(Window));
        public static RoutedCommand RestoreLast = new RoutedCommand("RestoreLast", typeof(Window));
        public static RoutedCommand Refresh = new RoutedCommand("Refresh", typeof(Window));
        public static RoutedCommand Delete = new RoutedCommand("Delete", typeof(Window));
        public static RoutedCommand Exit = new RoutedCommand("Exit", typeof(Window));
        public static RoutedCommand About = new RoutedCommand("About", typeof(Window));

        public static void RaiseClickEvent(this Control sender, ExecutedRoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                (sender as MenuItem)?.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
            else if (sender is ButtonBase)
            {
                (sender as ButtonBase)?.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }
    }
}

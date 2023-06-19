using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace BsaBrowser
{
    public static class RoutedCommands
    {
        public static RoutedCommand Refresh = new RoutedCommand("Refresh", typeof(Window));
        public static RoutedCommand Delete = new RoutedCommand("Delete", typeof(Window));
        public static RoutedCommand Exit = new RoutedCommand("Exit", typeof(Window));

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

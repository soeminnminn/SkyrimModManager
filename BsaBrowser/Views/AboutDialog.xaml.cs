using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace BsaBrowser.Views
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : Window
    {
        #region Variables
        private const string ABOUT_TEXT = "About";
        private const string CREDITS_TEXT = "Credits";
        private const string LICENSE_TEXT = "License";

        private const string LICENSE = "Licensed under the Apache License, Version 2.0 (the \"License\");\r\nyou may not use this file except in compliance with the License.\r\nYou may obtain a copy of the License at\r\n\r\nhttp://www.apache.org/licenses/LICENSE-2.0\r\n\r\nUnless required by applicable law or agreed to in writing, software distributed under the License is distributed on an \"AS IS\" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.";
        #endregion

        #region Properties
        public static readonly DependencyProperty TabSelectedProperty = DependencyProperty.Register(
            nameof(TabSelected), typeof(int), typeof(AboutDialog),
            new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty IsFirstButtonProperty = DependencyProperty.Register(
            nameof(IsFirstButton), typeof(bool), typeof(AboutDialog),
            new FrameworkPropertyMetadata(true, OnIsFirstButtonChanged));

        public static readonly DependencyProperty IsSecondButtonProperty = DependencyProperty.Register(
            nameof(IsSecondButton), typeof(bool), typeof(AboutDialog),
            new FrameworkPropertyMetadata(false, OnIsSecondButtonChanged));

        public static readonly DependencyProperty IsThirdButtonProperty = DependencyProperty.Register(
            nameof(IsThirdButton), typeof(bool), typeof(AboutDialog),
            new FrameworkPropertyMetadata(false, OnIsThirdButtonChanged));

        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
            nameof(Version), typeof(string), typeof(AboutDialog),
            new FrameworkPropertyMetadata("1.0.0.0"));

        public static readonly DependencyProperty CopyrightProperty = DependencyProperty.Register(
            nameof(Copyright), typeof(string), typeof(AboutDialog),
            new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty CreditsProperty = DependencyProperty.Register(
            nameof(Credits), typeof(string), typeof(AboutDialog),
            new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty LicenseProperty = DependencyProperty.Register(
            nameof(License), typeof(string), typeof(AboutDialog),
            new FrameworkPropertyMetadata(LICENSE));

        public int TabSelected
        {
            get => (int)GetValue(TabSelectedProperty);
            set { SetValue(TabSelectedProperty, value); }
        }

        public bool IsFirstButton
        {
            get => (bool)GetValue(IsFirstButtonProperty);
            set { SetValue(IsFirstButtonProperty, value); }
        }

        private static void OnIsFirstButtonChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is AboutDialog sender && (bool)e.NewValue == true)
            {
                sender.IsSecondButton = false;
                sender.IsThirdButton = false;
                sender.TabSelected = 0;
            }
        }

        public bool IsSecondButton
        {
            get => (bool)GetValue(IsSecondButtonProperty);
            set { SetValue(IsSecondButtonProperty, value); }
        }

        private static void OnIsSecondButtonChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is AboutDialog sender && (bool)e.NewValue == true)
            {
                sender.IsFirstButton = false;
                sender.IsThirdButton = false;
                sender.TabSelected = 1;
            }
        }

        public bool IsThirdButton
        {
            get => (bool)GetValue(IsThirdButtonProperty);
            set { SetValue(IsThirdButtonProperty, value); }
        }

        private static void OnIsThirdButtonChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is AboutDialog sender && (bool)e.NewValue == true)
            {
                sender.IsFirstButton = false;
                sender.IsSecondButton = false;
                sender.TabSelected = !string.IsNullOrEmpty(sender.License) ? 2 : 1;
            }
        }

        public bool HasButtons
        {
            get => !string.IsNullOrEmpty(Credits) || !string.IsNullOrEmpty(License);
        }

        public bool HasSecondButton
        {
            get => !string.IsNullOrEmpty(Credits) && !string.IsNullOrEmpty(License);
        }

        public string FirstButtonText
        {
            get => ABOUT_TEXT;
        }

        public string SecondButtonText
        {
            get => CREDITS_TEXT;
        }

        public string ThirdButtonText
        {
            get => !string.IsNullOrEmpty(License) ? LICENSE_TEXT : CREDITS_TEXT;
        }

        public string Version
        {
            get => (string)GetValue(VersionProperty);
            set { SetValue(VersionProperty, value); }
        }

        public string Copyright
        {
            get => (string)GetValue(CopyrightProperty);
            set { SetValue(CopyrightProperty, value); }
        }

        public string Credits
        {
            get => (string)GetValue(CreditsProperty);
            set { SetValue(CreditsProperty, value); }
        }

        public string License
        {
            get => (string)GetValue(LicenseProperty);
            set { SetValue(LicenseProperty, value); }
        }
        #endregion

        #region Constructors
        public AboutDialog()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, CloseWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, ShowSystemMenu));

            var assm = Assembly.GetExecutingAssembly();
            var assmName = assm.GetName();

            Version = assmName.Version.ToString();
            Title = assmName.Name;

            List<string> copyright = new();
            var companyAttr = assm.GetCustomAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
            if (companyAttr != null)
            {
                copyright.Add("Develop by " + companyAttr.Company);
            }

            var copyRightAttr = assm.GetCustomAttribute(typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute;
            if (copyRightAttr != null)
            {
                copyright.Add(copyRightAttr.Copyright);
            }

            if (copyright.Count > 0)
            {
                Copyright = string.Join(Environment.NewLine + Environment.NewLine, copyright.ToArray());
            }
        }
        #endregion

        #region Methods
        private void CloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowSystemMenu(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element)
            {
                var point = WindowState == WindowState.Maximized ? new Point(0, element.ActualHeight)
                    : new Point(Left + BorderThickness.Left, element.ActualHeight + Top + BorderThickness.Top);
                point = element.TransformToAncestor(this).Transform(point);
                SystemCommands.ShowSystemMenu(this, point);
            }
        }

        private void Caption_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (e.OriginalSource is FrameworkElement element)
                {
                    var mousePos = Mouse.GetPosition(this);
                    var point = WindowState == WindowState.Maximized ? mousePos :
                        new Point(mousePos.X + Left + BorderThickness.Left, mousePos.Y + Top + BorderThickness.Top);

                    point = element.TransformToAncestor(this).Transform(point);
                    SystemCommands.ShowSystemMenu(this, point);
                }
            }
        }
        #endregion
    }
}

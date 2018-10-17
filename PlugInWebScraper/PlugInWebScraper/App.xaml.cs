using PlugInWebScraper.ViewModels;
using PlugInWebScraper.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PlugInWebScraper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static List<ResourceDictionary> DarkResources
        {
            get
            {
                return new List<ResourceDictionary>()
                {
                    new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/Selen.Wpf.Core;component/MetroWindowResources.xaml", UriKind.Absolute)
                    },
                    new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml", UriKind.Absolute)
                    },
                    new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.Tooltip.xaml", UriKind.Absolute)
                    },
                    new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.Buttons.xaml", UriKind.Absolute)
                    },
                    new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/Selen.Wpf.Core;component/Resources.xaml", UriKind.Absolute)
                    },
                    new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/Selen.Wpf.SystemStyles;component/Styles.xaml", UriKind.Absolute)
                    },
                    new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/PlugInWebScraper;component/Styles/DarkStyleDictionary.xaml", UriKind.Absolute)
                    },
                };
            }
        }
        public static List<ResourceDictionary> LightResources
        {
            get
            {
                return new List<ResourceDictionary>()
                {
                    new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/PlugInWebScraper;component/Styles/LightStyleDictionary.xaml", UriKind.Absolute)
                    },
                };
            }
        }
        public static MainViewModel PrimaryViewModel
        {
            get; set;
        }
        public static Window PrimaryWindow
        {
            get;set;
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            PlugInWebScraper.Properties.Settings.Default.Save();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            PrimaryViewModel = new MainViewModel();
            WindowSwitch(Convert.ToBoolean(PlugInWebScraper.Properties.Settings.Default["IsDarkMode"]));
            PrimaryWindow.Show();
        }

        public static void ToggleTheme(bool isChecked)
        {
            Window old = PrimaryWindow;
            WindowSwitch(isChecked);
            old.Close();
            PrimaryWindow.Show();
        }


        private static void WindowSwitch(bool isChecked)
        {
            if (isChecked)
            {
                PrimaryWindow = new MainWindow()
                {
                    DataContext = PrimaryViewModel
                };
                Application.Current.Resources.MergedDictionaries.Clear();
                foreach (var r in DarkResources)
                {
                    Application.Current.Resources.MergedDictionaries.Add(r);
                }
            }
            else
            {
                PrimaryWindow = new LightWindow()
                {
                    DataContext = PrimaryViewModel
                };
                Application.Current.Resources.MergedDictionaries.Clear();
                foreach (var r in LightResources)
                {
                    Application.Current.Resources.MergedDictionaries.Add(r);
                }
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _changelog;
        public string Changelog
        {
            get => _changelog;
            set
            {
                _changelog = value;
                OnPropertyChanged(nameof(Changelog));
            }
        }


        private readonly List<string> didYouKnowStrings = [
            "You can open any existing addon and it will load all properties such as heels or hats.",
            "You can export an existing project when you are not finished and later import it to continue working on it.",
            "There is switch to enable dark theme in the settings.",
            "There is 'live texture' feature in 3d preview? It allows you to see how your texture looks on the model in real time, even after changes.",
            "You can click SHIFT + DEL to instantly delete a selected drawable, without popup.",
            "You can click CTRL + DEL to instantly replace a selected drawable with reserved drawable.",
            "You can reserve your drawables and later change it to real model.",
            "You can hover over warning icon to see what is wrong with your drawable or texture.",
        ];

        public string RandomDidYouKnow => didYouKnowStrings[new Random().Next(0, didYouKnowStrings.Count)];

        public Home()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += Home_Loaded;
        }

        private void Home_Loaded(object sender, RoutedEventArgs e)
        {
            // Load modern changelog
            LoadChangelog();
        }

        private void LoadChangelog()
        {
            ChangelogContent.Children.Clear();

            // Version 1.0.4 - UI Modernization
            AddChangelogVersion("1.0.4", "UI Modernization Update", new List<string>
            {
                "Modernized main window with new menu bar and bottom toolbar",
                "Added dark/light theme support with Visual Studio-inspired colors",
                "Redesigned project window with modern search box and improved layout",
                "Added Material Design icons throughout the application",
                "Implemented modern button styles with hover and press states",
                "Added error list window with dynamic badge counters",
                "Improved settings window with consistent styling",
                "Enhanced data grid and tab control visuals",
                "Removed legacy licensing system",
                "Improved overall consistency and usability"
            });

            // Version 1.0.3
            AddChangelogVersion("1.0.3", "Bug Fixes and Improvements", new List<string>
            {
                "Added more sentry logging for most common crashes",
                "Reverted one bugfix that was supposed to fix 3d preview bug but caused more issues"
            });

            // Version 1.0.2
            AddChangelogVersion("1.0.2", "Stability Improvements", new List<string>
            {
                "Removed Newtonsoft.Json dependency which caused a lot of issues (thanks to sentry logging)",
                "Fixed one more crash: 'Cannot set Owner property to a Window that has already been shown'"
            });

            // Version 1.0.1
            AddChangelogVersion("1.0.1", "Initial Release Updates", new List<string>
            {
                "Added Sentry to log crashes",
                "Added Texture name for 'Invalid slice pitch' error, so maybe it will be easier to fix",
                "Fixed rare bug that would cause deadlock (freeze tool)",
                "Fixed 'Index was out of range' crash when changing drawables when reserve drawable was selected",
                "Fixed drawable path, it can be selected now",
                "Fixed limit of textures for 26 maximum",
                "Fixed 3d preview for jpg/png/dds textures",
                "A lot of crash fixes",
                "And a lot of different fixes and small features that I don't even remember anymore"
            });
        }

        private void AddChangelogVersion(string version, string title, List<string> changes)
        {
            // Version header
            var versionPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
            
            var versionText = new TextBlock
            {
                Text = $"Version {version}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("Brush950")
            };
            versionPanel.Children.Add(versionText);

            var separator = new TextBlock
            {
                Text = " - ",
                FontSize = 16,
                Foreground = (System.Windows.Media.Brush)FindResource("Brush700")
            };
            versionPanel.Children.Add(separator);

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBlueBrush")
            };
            versionPanel.Children.Add(titleText);

            ChangelogContent.Children.Add(versionPanel);

            // Changes list
            foreach (var change in changes)
            {
                var changePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 2, 0, 2) };
                
                var bullet = new TextBlock
                {
                    Text = "•",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 8, 0),
                    Foreground = (System.Windows.Media.Brush)FindResource("AccentBlueBrush"),
                    VerticalAlignment = VerticalAlignment.Top
                };
                changePanel.Children.Add(bullet);

                var changeText = new TextBlock
                {
                    Text = change,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (System.Windows.Media.Brush)FindResource("Brush900"),
                    LineHeight = 18
                };
                changePanel.Children.Add(changeText);

                ChangelogContent.Children.Add(changePanel);
            }
        }

        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            var (result, textBoxValue) = Show("Choose a name for your project", "Project Name", CustomMessageBoxButtons.OKCancel, CustomMessageBoxIcon.None, true);
            if (result == CustomMessageBoxResult.OK)
            {
                MainWindow.AddonManager.ProjectName = textBoxValue;
            }

            MainWindow.AddonManager.CreateAddon();
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private async void OpenAddon_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.Instance.OpenAddonAsync(true);
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private async void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.Instance.ImportProjectAsync(true);
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

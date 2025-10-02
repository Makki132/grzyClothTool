using System.Windows;
using System.Windows.Controls;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for SearchBox.xaml
    /// </summary>
    public partial class SearchBox : UserControl
    {
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(SearchBox), 
                new PropertyMetadata("Search..."));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(SearchBox), 
                new PropertyMetadata(string.Empty));

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        // Event for when search text changes
        public event RoutedEventHandler SearchTextChanged;

        public SearchBox()
        {
            InitializeComponent();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = searchTextBox.Text;

            // Show/hide placeholder
            placeholderText.Visibility = string.IsNullOrEmpty(searchTextBox.Text) 
                ? Visibility.Visible 
                : Visibility.Collapsed;

            // Show/hide clear button
            clearButton.Visibility = string.IsNullOrEmpty(searchTextBox.Text) 
                ? Visibility.Collapsed 
                : Visibility.Visible;

            // Raise event
            SearchTextChanged?.Invoke(this, new RoutedEventArgs());
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            searchTextBox.Clear();
            searchTextBox.Focus();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            placeholderText.Visibility = Visibility.Collapsed;
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(searchTextBox.Text))
            {
                placeholderText.Visibility = Visibility.Visible;
            }
        }

        public void Focus()
        {
            searchTextBox.Focus();
        }

        public void Clear()
        {
            searchTextBox.Clear();
        }
    }
}

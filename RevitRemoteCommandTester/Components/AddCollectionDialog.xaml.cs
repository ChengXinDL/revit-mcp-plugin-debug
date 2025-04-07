// AddCollectionDialog.xaml.cs
using System.Windows;

namespace RevitRemoteCommandTester.Components
{
    public partial class AddCollectionDialog : Window
    {
        public string? CollectionName { get; private set; }

        public AddCollectionDialog(string defaultName = "")
        {
            InitializeComponent();
            NameTextBox.Text = defaultName;
            NameTextBox.SelectAll();
            NameTextBox.Focus();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for the collection.", "Name Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CollectionName = NameTextBox.Text;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

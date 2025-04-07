// AddCommandDialog.xaml.cs
using System.Windows;

namespace RevitRemoteCommandTester.Components
{
    public partial class AddCommandDialog : Window
    {
        public string? CommandName { get; private set; }

        public AddCommandDialog(string defaultName = "")
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
                MessageBox.Show("Please enter a name for the command.", "Name Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CommandName = NameTextBox.Text;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

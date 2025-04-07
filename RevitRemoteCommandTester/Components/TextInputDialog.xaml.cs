using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RevitRemoteCommandTester.Components
{
    /// <summary>
    /// TextInputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class TextInputDialog : Window
    {
        public string InputText { get; private set; }

        public TextInputDialog(string title, string prompt)
        {
            InitializeComponent();

            Title = title;
            PromptText.Text = prompt;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

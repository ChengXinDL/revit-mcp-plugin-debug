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
    /// TextOutputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class TextOutputDialog : Window
    {
        public TextOutputDialog(string title, string prompt, string outputText)
        {
            InitializeComponent();

            Title = title;
            PromptText.Text = prompt;
            OutputTextBox.Text = outputText;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OutputTextBox.Text);
            CopyButton.Content = "Copied!";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

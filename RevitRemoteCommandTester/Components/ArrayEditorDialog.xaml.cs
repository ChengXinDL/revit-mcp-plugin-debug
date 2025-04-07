using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// ArrayEditorDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ArrayEditorDialog : Window
    {
        public string ParameterName { get; private set; }
        public JArray ArrayValue { get; private set; }
        public ObservableCollection<ArrayItem> Items { get; set; }
        public ArrayEditorDialog(string parameterName, object initialValue)
        {
            InitializeComponent();

            ParameterName = parameterName;
            ParameterNameText.Text = parameterName;

            Items = new ObservableCollection<ArrayItem>();
            ArrayItemsControl.ItemsSource = Items;

            // 尝试将初始值解析为JArray
            if (initialValue is JArray jArray)
            {
                ArrayValue = jArray;
            }
            else if (initialValue != null)
            {
                try
                {
                    ArrayValue = JArray.Parse(initialValue.ToString());
                }
                catch
                {
                    ArrayValue = new JArray();
                }
            }
            else
            {
                ArrayValue = new JArray();
            }

            // 初始化数组项列表
            foreach (var item in ArrayValue)
            {
                Items.Add(new ArrayItem(item));
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            Items.Add(new ArrayItem(""));
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ArrayItem item)
            {
                Items.Remove(item);
            }
        }

        private void MoveItemUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ArrayItem item)
            {
                int index = Items.IndexOf(item);
                if (index > 0)
                {
                    Items.Move(index, index - 1);
                }
            }
        }

        private void MoveItemDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is ArrayItem item)
            {
                int index = Items.IndexOf(item);
                if (index < Items.Count - 1)
                {
                    Items.Move(index, index + 1);
                }
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // 构建新的JArray
            ArrayValue = new JArray();

            foreach (var item in Items)
            {
                // 尝试解析值为JSON
                try
                {
                    var value = JsonConvert.DeserializeObject(item.Value.ToString());
                    ArrayValue.Add(JToken.FromObject(value));
                }
                catch
                {
                    // 如果无法解析，则视为字符串
                    ArrayValue.Add(item.Value.ToString());
                }
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ArrayItem
    {
        public object Value { get; set; }

        public ArrayItem(object value)
        {
            Value = value;
        }
    }

    // 用于显示数组索引的转换器
    public class ItemIndexConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ContentPresenter cp = value as ContentPresenter;
            if (cp == null) return 0;

            ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(cp);
            if (itemsControl == null) return 0;

            int index = itemsControl.ItemContainerGenerator.IndexFromContainer(cp);
            return index.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

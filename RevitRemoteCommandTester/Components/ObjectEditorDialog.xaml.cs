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
    /// ObjectEditorDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ObjectEditorDialog : Window
    {
        public string ParameterName { get; private set; }
        public JObject ObjectValue { get; private set; }
        public ObservableCollection<KeyValuePair> Properties { get; set; }
        public ObjectEditorDialog(string parameterName, object initialValue)
        {
            InitializeComponent();

            ParameterName = parameterName;
            ParameterNameText.Text = parameterName;

            Properties = new ObservableCollection<KeyValuePair>();
            PropertiesItemsControl.ItemsSource = Properties;

            // 尝试将初始值解析为JObject
            if (initialValue is JObject jObject)
            {
                ObjectValue = jObject;
            }
            else if (initialValue != null)
            {
                try
                {
                    ObjectValue = JObject.Parse(initialValue.ToString());
                }
                catch
                {
                    ObjectValue = new JObject();
                }
            }
            else
            {
                ObjectValue = new JObject();
            }

            // 初始化属性列表
            foreach (var prop in ObjectValue.Properties())
            {
                Properties.Add(new KeyValuePair(prop.Name, prop.Value));
            }
        }

        private void AddProperty_Click(object sender, RoutedEventArgs e)
        {
            Properties.Add(new KeyValuePair("", ""));
        }

        private void RemoveProperty_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is KeyValuePair pair)
            {
                Properties.Remove(pair);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // 构建新的JObject
            ObjectValue = new JObject();

            foreach (var prop in Properties)
            {
                if (!string.IsNullOrWhiteSpace(prop.Key))
                {
                    // 尝试解析值为JSON
                    try
                    {
                        var value = JsonConvert.DeserializeObject(prop.Value.ToString());
                        ObjectValue[prop.Key] = JToken.FromObject(value);
                    }
                    catch
                    {
                        // 如果无法解析，则视为字符串
                        ObjectValue[prop.Key] = prop.Value.ToString();
                    }
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

    public class KeyValuePair
    {
        public string Key { get; set; }
        public object Value { get; set; }

        public KeyValuePair(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}

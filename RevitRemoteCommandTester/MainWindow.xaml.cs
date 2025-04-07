using RevitRemoteCommandTester.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Net.Sockets;
using System.Text;
using RevitRemoteCommandTester.Services;
using Newtonsoft.Json.Linq;
using RevitRemoteCommandTester.Components;

namespace RevitRemoteCommandTester
{
    public partial class MainWindow : Window
    {
        // 添加服务器配置属性
        public string ServerAddress { get; set; } = "localhost";
        public int ServerPort { get; set; } = 8080;

        // tcp通讯服务
        private TcpCommunicationService communicationService;

        public ObservableCollection<Collection> Collections { get; set; }
        private Command? currentCommand;
        public ObservableCollection<Parameter> Parameters { get; set; }
        private Popup? currentOpenPopup; // 跟踪当前打开的弹出菜单

        // 字段用于存储数组项
        private Dictionary<string, ObservableCollection<string>> arrayItems = new Dictionary<string, ObservableCollection<string>>();

        public MainWindow()
        {
            InitializeComponent();

            Collections = new ObservableCollection<Collection>();
            Parameters = new ObservableCollection<Parameter>();

            NavigationTreeView.ItemsSource = Collections;
            ParametersItemsControl.ItemsSource = Parameters;

            communicationService = new TcpCommunicationService(ServerAddress, ServerPort);
        }

        private void AddCollection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCollectionDialog($"Collection {Collections.Count + 1}");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var collection = new Collection(dialog.CollectionName);
                Collections.Add(collection);
            }
        }

        private void AddTool_Click(object sender, RoutedEventArgs e)
        {
            // 关闭任何打开的弹出菜单
            CloseCurrentPopup();

            var button = sender as Button;
            if (button == null) return;
            var collection = button.Tag as Collection;
            if (collection == null) return;

            var dialog = new AddCommandDialog($"Command {collection.Commands.Count + 1}");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var command = new Command(dialog.CommandName);
                collection.Commands.Add(command);
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // 关闭任何打开的弹出菜单
            CloseCurrentPopup();
            // 重置所有面板的可见性
            CollectionInfoPanel.Visibility = Visibility.Collapsed;
            CommandParametersPanel.Visibility = Visibility.Collapsed;
            SendCommandButton.Visibility = Visibility.Collapsed;
            if (e.NewValue is Command command)
            {
                currentCommand = command;
                SelectedItemName.Text = command.Name;
                DisplayCommandProperties();
                // 显示命令参数面板
                CommandParametersPanel.Visibility = Visibility.Visible;
                SendCommandButton.Visibility = Visibility.Visible;
            }
            else if (e.NewValue is Collection collection)
            {
                // 如果选择的是集合，显示集合信息
                currentCommand = null;
                SelectedItemName.Text = collection.Name;
                // 更新集合信息
                CommandCount.Text = collection.Commands.Count.ToString();
                AddCommandButton.Tag = collection;
                // 显示集合信息面板
                CollectionInfoPanel.Visibility = Visibility.Visible;
            }
            else
            {
                // 如果没有选择任何内容
                currentCommand = null;
                SelectedItemName.Text = "";
            }
        }

        private void DisplayCommandProperties()
        {
            if (currentCommand != null)
            {
                Parameters.Clear();
                if (currentCommand.Parameters != null)
                {
                    foreach (var param in currentCommand.Parameters)
                    {
                        Parameters.Add(new Parameter(param.Key, param.Value));
                    }
                }
            }
        }

        // 从集合信息面板添加命令的新方法
        private void AddCommand_FromInfoPanel(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var collection = button.Tag as Collection;
            if (collection == null) return;
            var dialog = new AddCommandDialog($"Command {collection.Commands.Count + 1}");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var command = new Command(dialog.CommandName);
                collection.Commands.Add(command);

                // 更新命令计数
                CommandCount.Text = collection.Commands.Count.ToString();
            }
        }

        private void AddParameter_Click(object sender, RoutedEventArgs e)
        {
            Parameters.Add(new Parameter("", ""));
        }

        private void RemoveParameter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Parameter parameter)
            {
                Parameters.Remove(parameter);
            }
        }

        private async void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            if (currentCommand == null)
            {
                MessageBox.Show("Please select a command first.", "No Command Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 更新命令参数
            currentCommand.Parameters = new Dictionary<string, object>();

            foreach (var param in Parameters)
            {
                if (!string.IsNullOrWhiteSpace(param.Key))
                {
                    // 查找类型选择器
                    var typeSelector = FindParameterTypeSelector(param.Key);
                    if (typeSelector != null)
                    {
                        var selectedItem = typeSelector.SelectedItem as ComboBoxItem;
                        var valueType = selectedItem?.Tag as string;

                        switch (valueType)
                        {
                            case "array":
                                if (arrayItems.ContainsKey(param.Key))
                                {
                                    // 将ObservableCollection转换为数组
                                    currentCommand.Parameters[param.Key] = arrayItems[param.Key].ToArray();
                                }
                                else
                                {
                                    currentCommand.Parameters[param.Key] = new string[0];
                                }
                                break;

                            case "number":
                                if (decimal.TryParse(param.Value.ToString(), out decimal numValue))
                                    currentCommand.Parameters[param.Key] = numValue;
                                else
                                    currentCommand.Parameters[param.Key] = 0;
                                break;

                            case "boolean":
                                if (bool.TryParse(param.Value.ToString(), out bool boolValue))
                                    currentCommand.Parameters[param.Key] = boolValue;
                                else
                                    currentCommand.Parameters[param.Key] = false;
                                break;

                            case "null":
                                currentCommand.Parameters[param.Key] = null;
                                break;

                            case "json":
                                try
                                {
                                    currentCommand.Parameters[param.Key] = JsonConvert.DeserializeObject(param.Value.ToString());
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Invalid JSON for parameter '{param.Key}': {ex.Message}",
                                        "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                                break;

                            default: // text
                                currentCommand.Parameters[param.Key] = param.Value;
                                break;
                        }
                    }
                    else
                    {
                        // 默认作为文本处理
                        currentCommand.Parameters[param.Key] = param.Value;
                    }
                }
            }

            // 构建符合JSON-RPC 2.0规范的请求
            var jsonRpcRequest = new
            {
                jsonrpc = "2.0",
                method = currentCommand.Name,
                @params = currentCommand.Parameters,
                id = 1
            };
            string commandJson = JsonConvert.SerializeObject(jsonRpcRequest, Formatting.Indented);

            try
            {
                ResponsePanel.Visibility = Visibility.Visible;
                // 显示正在发送的命令
                ResponseTextBox.Text = $"Sending command:\n{commandJson}\n\nWaiting for response...";

                // 异步发送命令并获取响应
                string response = await communicationService.SendRawCommandAsync(commandJson);

                // 显示响应
                ResponseTextBox.Text = $"Command sent:\n{commandJson}\n\nResponse:\n{response}";
            }
            catch (Exception ex)
            {
                ResponseTextBox.Text = $"Error sending command:\n{ex.Message}";
                MessageBox.Show($"Failed to send command: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ComboBox FindParameterTypeSelector(string paramKey)
        {
            // 这里需要根据您的UI结构实现查找逻辑
            // 简单实现示例，可能需要根据您的实际UI结构调整
            foreach (var child in ParametersItemsControl.Items)
            {
                var container = ParametersItemsControl.ItemContainerGenerator.ContainerFromItem(child);
                if (container != null)
                {
                    var grid = FindVisualChild<Grid>(container);
                    if (grid != null)
                    {
                        var keyTextBox = FindChild<TextBox>(grid, "KeyTextBox");
                        if (keyTextBox != null && keyTextBox.Text == paramKey)
                        {
                            return FindChild<ComboBox>(grid, "ValueTypeSelector");
                        }
                    }
                }
            }
            return null;
        }

        // 通用的查找子控件方法
        private T FindChild<T>(DependencyObject parent, string childName = null) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild &&
                    (childName == null || (child is FrameworkElement fe && fe.Name == childName)))
                {
                    return typedChild;
                }

                var result = FindChild<T>(child, childName);
                if (result != null)
                    return result;
            }
            return null;
        }

        // 查找第一个指定类型的Visual子元素
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        // 查找父控件
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private Grid FindParentGrid(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is Grid))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as Grid;
        }

        // 添加数组项
        private void AddArrayItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // 向上查找参数键和对应的数组集合
            var arrayBorder = FindParent<Border>(button);
            var parameterGrid = FindParent<Grid>(arrayBorder);
            var keyTextBox = FindChild<TextBox>(parameterGrid, "KeyTextBox");

            if (keyTextBox != null)
            {
                string key = keyTextBox.Text;
                if (arrayItems.ContainsKey(key))
                {
                    arrayItems[key].Add("");
                }
            }
        }

        // 删除数组项
        private void RemoveArrayItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var item = button.Tag as string;
            var arrayBorder = FindParent<Border>(button);
            var parameterGrid = FindParent<Grid>(arrayBorder);
            var keyTextBox = FindChild<TextBox>(parameterGrid, "KeyTextBox");

            if (keyTextBox != null)
            {
                string key = keyTextBox.Text;
                if (arrayItems.ContainsKey(key))
                {
                    arrayItems[key].Remove(item);
                }
            }
        }

        #region 集合和命令操作方法

        private void ShowCollectionOptions_Click(object sender, RoutedEventArgs e)
        {
            // 关闭任何已打开的弹出菜单
            CloseCurrentPopup();

            var button = sender as Button;
            if (button == null) return;

            // 找到当前按钮关联的 Popup
            var parent = VisualTreeHelper.GetParent(button);
            while (parent != null)
            {
                if (parent is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Popup popup)
                        {
                            currentOpenPopup = popup;
                            popup.IsOpen = true;
                            e.Handled = true;
                            return;
                        }
                    }
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        private void ShowCommandOptions_Click(object sender, RoutedEventArgs e)
        {
            // 关闭任何已打开的弹出菜单
            CloseCurrentPopup();

            var button = sender as Button;
            if (button == null) return;

            // 找到当前按钮关联的 Popup
            var parent = VisualTreeHelper.GetParent(button);
            while (parent != null)
            {
                if (parent is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Popup popup)
                        {
                            currentOpenPopup = popup;
                            popup.IsOpen = true;
                            e.Handled = true;
                            return;
                        }
                    }
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        private void CloseCurrentPopup()
        {
            if (currentOpenPopup != null && currentOpenPopup.IsOpen)
            {
                currentOpenPopup.IsOpen = false;
                currentOpenPopup = null;
            }
        }

        private void RenameCollection_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentPopup();

            var button = sender as Button;
            if (button == null) return;
            var collection = button.Tag as Collection;
            if (collection == null) return;

            var dialog = new AddCollectionDialog(collection.Name);
            dialog.Title = "Rename Collection";
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                collection.Name = dialog.CollectionName;
            }
        }

        private void DeleteCollection_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentPopup();
            var button = sender as Button;
            if (button == null) return;
            var collection = button.Tag as Collection;
            if (collection == null) return;
            var result = MessageBox.Show($"Are you sure you want to delete collection '{collection.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                Collections.Remove(collection);
                // 如果当前选择的命令属于这个集合，清空右侧面板
                if (currentCommand != null && collection.Commands.Contains(currentCommand))
                {
                    currentCommand = null;
                    SelectedItemName.Text = "";  // 修正为 SelectedItemName
                    Parameters.Clear();
                    // 隐藏参数面板
                    CommandParametersPanel.Visibility = Visibility.Collapsed;
                    SendCommandButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void RenameCommand_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentPopup();
            var button = sender as Button;
            if (button == null) return;
            var command = button.Tag as Command;
            if (command == null) return;
            var dialog = new AddCommandDialog(command.Name);
            dialog.Title = "Rename Command";
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                command.Name = dialog.CommandName;
                // 如果重命名的是当前选中的命令，更新标题
                if (command == currentCommand)
                {
                    // 使用SelectedItemName而不是SelectedCommandName
                    SelectedItemName.Text = command.Name;
                }
            }
        }

        private void DeleteCommand_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentPopup();
            var button = sender as Button;
            if (button == null) return;
            var command = button.Tag as Command;
            if (command == null) return;
            var result = MessageBox.Show($"Are you sure you want to delete command '{command.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                // 找到该命令所属的集合
                foreach (var collection in Collections)
                {
                    if (collection.Commands.Contains(command))
                    {
                        collection.Commands.Remove(command);
                        // 如果删除的是当前选中的命令，清空右侧面板
                        if (command == currentCommand)
                        {
                            currentCommand = null;
                            SelectedItemName.Text = "";  // 修正为 SelectedItemName
                            Parameters.Clear();

                            // 隐藏参数面板
                            CommandParametersPanel.Visibility = Visibility.Collapsed;
                            SendCommandButton.Visibility = Visibility.Collapsed;
                        }

                        break;
                    }
                }
            }
        }

        // 对象值编辑器弹窗
        private void EditObjectValue_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var parameter = button.Tag as Parameter;
            if (parameter == null) return;

            // 创建一个对象编辑器弹窗
            var dialog = new ObjectEditorDialog(parameter.Key, parameter.Value);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                parameter.Value = dialog.ObjectValue;
                UpdateJsonPreview();
            }
        }
        // 数组值编辑器弹窗
        private void EditArrayValue_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var parameter = button.Tag as Parameter;
            if (parameter == null) return;

            // 创建一个数组编辑器弹窗
            var dialog = new ArrayEditorDialog(parameter.Key, parameter.Value);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                parameter.Value = dialog.ArrayValue;
                UpdateJsonPreview();
            }
        }
        // 类型选择改变事件处理
        private void ValueTypeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            var valueType = selectedItem.Tag as string;
            var parameter = comboBox.Tag as Parameter;
            if (parameter == null) return;

            // 找到相关控件
            var container = comboBox.TemplatedParent as ContentPresenter;
            if (container == null) return;

            var grid = container.ContentTemplate.FindName("Grid", container) as Grid;
            if (grid == null) return;

            var stringEditor = FindChild<TextBox>(grid, "StringValueEditor");
            var numberEditor = FindChild<TextBox>(grid, "NumberValueEditor");
            var booleanEditor = FindChild<ComboBox>(grid, "BooleanValueEditor");
            var objectButton = FindChild<Button>(grid, "ObjectEditorButton");
            var arrayButton = FindChild<Button>(grid, "ArrayEditorButton");
            var nullDisplay = FindChild<TextBlock>(grid, "NullValueDisplay");

            // 隐藏所有编辑器
            if (stringEditor != null) stringEditor.Visibility = Visibility.Collapsed;
            if (numberEditor != null) numberEditor.Visibility = Visibility.Collapsed;
            if (booleanEditor != null) booleanEditor.Visibility = Visibility.Collapsed;
            if (objectButton != null) objectButton.Visibility = Visibility.Collapsed;
            if (arrayButton != null) arrayButton.Visibility = Visibility.Collapsed;
            if (nullDisplay != null) nullDisplay.Visibility = Visibility.Collapsed;

            // 根据选择的类型显示对应的编辑器
            switch (valueType)
            {
                case "string":
                    if (stringEditor != null) stringEditor.Visibility = Visibility.Visible;
                    break;
                case "number":
                    if (numberEditor != null) numberEditor.Visibility = Visibility.Visible;
                    // 尝试转换为数字格式
                    if (parameter.Value != null && decimal.TryParse(parameter.Value.ToString(), out decimal numValue))
                        numberEditor.Text = numValue.ToString();
                    else
                        numberEditor.Text = "0";
                    break;
                case "boolean":
                    if (booleanEditor != null)
                    {
                        booleanEditor.Visibility = Visibility.Visible;
                        // 设置布尔值
                        bool isTrue = parameter.Value != null &&
                            bool.TryParse(parameter.Value.ToString(), out bool value) && value;
                        booleanEditor.SelectedIndex = isTrue ? 0 : 1;
                    }
                    break;
                case "object":
                    if (objectButton != null) objectButton.Visibility = Visibility.Visible;
                    break;
                case "array":
                    if (arrayButton != null) arrayButton.Visibility = Visibility.Visible;
                    break;
                case "null":
                    if (nullDisplay != null) nullDisplay.Visibility = Visibility.Visible;
                    parameter.Value = null;
                    break;
            }

            UpdateJsonPreview();
        }
        // 更新JSON预览
        private void UpdateJsonPreview()
        {
            if (currentCommand == null) return;

            // 更新命令参数
            var tempParams = new Dictionary<string, object>();

            foreach (var param in Parameters)
            {
                if (!string.IsNullOrWhiteSpace(param.Key))
                {
                    // 查找类型选择器
                    var typeSelector = FindParameterTypeSelector(param.Key);
                    if (typeSelector != null)
                    {
                        var selectedItem = typeSelector.SelectedItem as ComboBoxItem;
                        var valueType = selectedItem?.Tag as string;

                        switch (valueType)
                        {
                            case "string":
                                tempParams[param.Key] = param.Value?.ToString() ?? "";
                                break;
                            case "number":
                                if (decimal.TryParse(param.Value?.ToString(), out decimal numValue))
                                    tempParams[param.Key] = numValue;
                                else
                                    tempParams[param.Key] = 0;
                                break;
                            case "boolean":
                                if (bool.TryParse(param.Value?.ToString(), out bool boolValue))
                                    tempParams[param.Key] = boolValue;
                                else
                                    tempParams[param.Key] = false;
                                break;
                            case "object":
                                if (param.Value is JObject jObject)
                                    tempParams[param.Key] = jObject;
                                else if (param.Value != null)
                                    try { tempParams[param.Key] = JObject.Parse(param.Value.ToString()); }
                                    catch { tempParams[param.Key] = new JObject(); }
                                else
                                    tempParams[param.Key] = new JObject();
                                break;
                            case "array":
                                if (param.Value is JArray jArray)
                                    tempParams[param.Key] = jArray;
                                else if (param.Value != null)
                                    try { tempParams[param.Key] = JArray.Parse(param.Value.ToString()); }
                                    catch { tempParams[param.Key] = new JArray(); }
                                else
                                    tempParams[param.Key] = new JArray();
                                break;
                            case "null":
                                tempParams[param.Key] = null;
                                break;
                        }
                    }
                    else
                    {
                        // 默认作为字符串处理
                        tempParams[param.Key] = param.Value?.ToString() ?? "";
                    }
                }
            }

            // 构建JSON-RPC 2.0请求预览
            var jsonRpcRequest = new
            {
                jsonrpc = "2.0",
                method = currentCommand.Name,
                @params = tempParams,
                id = 1
            };

            string previewJson = JsonConvert.SerializeObject(jsonRpcRequest, Formatting.Indented);

            if (JsonPreviewTextBox != null)
            {
                JsonPreviewTextBox.Text = previewJson;
            }
        }
        // 导入参数
        private void ImportParameters_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputDialog("Import Parameters", "Paste JSON object for parameters:");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var paramsObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(dialog.InputText);
                    if (paramsObject != null)
                    {
                        Parameters.Clear();
                        foreach (var param in paramsObject)
                        {
                            Parameters.Add(new Parameter(param.Key, param.Value));
                        }
                        UpdateJsonPreview();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to parse JSON: {ex.Message}", "Import Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // 导出参数
        private void ExportParameters_Click(object sender, RoutedEventArgs e)
        {
            // 更新命令参数
            var tempParams = new Dictionary<string, object>();

            foreach (var param in Parameters)
            {
                if (!string.IsNullOrWhiteSpace(param.Key))
                {
                    var typeSelector = FindParameterTypeSelector(param.Key);
                    if (typeSelector != null)
                    {
                        var selectedItem = typeSelector.SelectedItem as ComboBoxItem;
                        var valueType = selectedItem?.Tag as string;

                        // 使用与UpdateJsonPreview相同的逻辑获取参数
                        // [此处省略相同的代码逻辑]
                    }
                }
            }

            string paramsJson = JsonConvert.SerializeObject(tempParams, Formatting.Indented);

            var dialog = new TextOutputDialog("Export Parameters", "Copy the JSON below:", paramsJson);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        #endregion
    }
}

using RevitRemoteCommandTester.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Newtonsoft.Json;
using System.Windows.Media;
using RevitRemoteCommandTester.Services;
using Newtonsoft.Json.Linq;
using RevitRemoteCommandTester.Components;

namespace RevitRemoteCommandTester
{
    public partial class MainWindow : Window
    {
        // 添加服务器配置属性
        public string ServerAddress { get; set; } = "localhost";
        public int ServerPort { get; set; } = 8082;

        //// tcp通讯服务
        //private TcpCommunicationService communicationService;
        // webSocket通讯服务
        private WebSocketCommunicationService communicationService;
        // 数据持久化服务
        private readonly DataPersistenceService dataPersistenceService;
        private bool isInitialLoad = true;

        public ObservableCollection<Collection> Collections { get; set; }
        private Command? currentCommand;
        public ObservableCollection<Parameter> Parameters { get; set; }
        private Popup? currentOpenPopup; // 跟踪当前打开的弹出菜单

        public MainWindow()
        {
            InitializeComponent();

            Collections = new ObservableCollection<Collection>();
            Parameters = new ObservableCollection<Parameter>();

            NavigationTreeView.ItemsSource = Collections;

            // 服务初始化
            //communicationService = new TcpCommunicationService(ServerAddress, ServerPort);
            communicationService = new WebSocketCommunicationService(ServerAddress, ServerPort);
            dataPersistenceService = new DataPersistenceService();

            // 加载数据
            LoadDataAsync();

            // 窗口关闭事件，用于保存数据
            Closing += MainWindow_Closing;
        }

        private void DisplayCommandProperties()
        {
            if (currentCommand != null && currentCommand.Parameters != null)
            {
                // 将命令参数转换为JSON格式并显示在文本框中
                string jsonParams = JsonConvert.SerializeObject(currentCommand.Parameters, Formatting.Indented);
                JsonParametersTextBox.Text = jsonParams;
            }
            else
            {
                JsonParametersTextBox.Text = "{}";
            }
        }

        // 载入数据
        private async void LoadDataAsync()
        {
            try
            {
                var loadedCollections = await dataPersistenceService.LoadCollectionsAsync();
                if (loadedCollections != null && loadedCollections.Count > 0)
                {
                    foreach (var collection in loadedCollections)
                    {
                        Collections.Add(collection);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Data Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isInitialLoad = false;
            }
        }

        // 保存数据
        private async void SaveDataAsync()
        {
            try
            {
                await dataPersistenceService.SaveCollectionsAsync(Collections);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving data: {ex.Message}", "Data Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 事件
        private void AddCollection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCollectionDialog($"Collection {Collections.Count + 1}");
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var collection = new Collection(dialog.CollectionName);
                Collections.Add(collection);

                // 保存数据
                if (!isInitialLoad) SaveDataAsync();
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

                // 保存数据
                if (!isInitialLoad) SaveDataAsync();
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

                // 保存数据
                if (!isInitialLoad) SaveDataAsync();
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
            try
            {
                // 尝试解析JSON参数
                JObject parameterObj;
                try
                {
                    parameterObj = JObject.Parse(JsonParametersTextBox.Text);
                    currentCommand.Parameters = parameterObj.ToObject<Dictionary<string, object>>();

                    // 参数更新后保存数据
                    if (!isInitialLoad) SaveDataAsync();
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Invalid JSON format: {ex.Message}", "JSON Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 修复：只在未连接时建立连接
                if (!communicationService.IsConnected)
                {
                    try
                    {
                        await communicationService.ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to connect to server: {ex.Message}", "Connection Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
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
        // JSON参数文本变化（检查格式）
        private void JsonParameters_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentCommand == null) return;
            try
            {
                // 检查JSON是否有效，但不立即更新命令参数
                JObject jsonObj = JObject.Parse(JsonParametersTextBox.Text);

                // 当参数实际变化时保存
                var newParameters = jsonObj.ToObject<Dictionary<string, object>>();
                string newJson = JsonConvert.SerializeObject(newParameters);
                string oldJson = currentCommand.Parameters != null ?
                    JsonConvert.SerializeObject(currentCommand.Parameters) : "null";

                if (newJson != oldJson)
                {
                    currentCommand.Parameters = newParameters;

                    // 保存数据
                    if (!isInitialLoad) SaveDataAsync();
                }
            }
            catch (JsonException)
            {
                // 如果JSON格式无效，暂不处理，等到用户点击发送按钮时处理
            }
        }
        // JSON格式化
        private void FormatJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 解析当前JSON文本并格式化
                JObject jsonObj = JObject.Parse(JsonParametersTextBox.Text);
                JsonParametersTextBox.Text = jsonObj.ToString(Formatting.Indented);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Invalid JSON format: {ex.Message}", "Format Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // 清除JSON输入
        private void ClearJson_Click(object sender, RoutedEventArgs e)
        {
            JsonParametersTextBox.Text = "{}";
        }
        // 窗口关闭（保存数据）
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveDataAsync();
        }
        #endregion

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

                // 保存数据
                if (!isInitialLoad) SaveDataAsync();
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
                    SelectedItemName.Text = "";
                    // 隐藏参数面板
                    CommandParametersPanel.Visibility = Visibility.Collapsed;
                    SendCommandButton.Visibility = Visibility.Collapsed;
                    ResponsePanel.Visibility = Visibility.Collapsed;

                    // 保存数据
                    if (!isInitialLoad) SaveDataAsync();
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
                    SelectedItemName.Text = command.Name;
                }

                // 保存数据
                if (!isInitialLoad) SaveDataAsync();
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
                            SelectedItemName.Text = "";
                            // 隐藏参数面板
                            CommandParametersPanel.Visibility = Visibility.Collapsed;
                            SendCommandButton.Visibility = Visibility.Collapsed;
                            ResponsePanel.Visibility = Visibility.Collapsed;
                        }

                        // 保存数据
                        if (!isInitialLoad) SaveDataAsync();
                        break;
                    }
                }
            }
        }
        #endregion
    }
}

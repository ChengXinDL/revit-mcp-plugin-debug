using Newtonsoft.Json;
using RevitRemoteCommandTester.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace RevitRemoteCommandTester.Services
{
    public class DataPersistenceService
    {
        private const string DATA_FOLDER = "data";
        private const string COLLECTIONS_FILE = "collections.json";
        private string DataFolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DATA_FOLDER);
        private string CollectionsFilePath => Path.Combine(DataFolderPath, COLLECTIONS_FILE);

        public DataPersistenceService()
        {
            // 确保数据文件夹存在
            if (!Directory.Exists(DataFolderPath))
            {
                Directory.CreateDirectory(DataFolderPath);
            }
        }

        /// <summary>
        /// 保存集合数据
        /// </summary>
        public async Task SaveCollectionsAsync(ObservableCollection<Collection> collections)
        {
            try
            {
                string json = JsonConvert.SerializeObject(collections, Formatting.Indented);
                await File.WriteAllTextAsync(CollectionsFilePath, json);
            }
            catch (Exception ex)
            {
                // 在实际应用中，您可能希望记录错误或通知用户
                System.Diagnostics.Debug.WriteLine($"Error saving collections: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载集合数据
        /// </summary>
        public async Task<ObservableCollection<Collection>> LoadCollectionsAsync()
        {
            try
            {
                if (!File.Exists(CollectionsFilePath))
                {
                    return new ObservableCollection<Collection>();
                }

                string json = await File.ReadAllTextAsync(CollectionsFilePath);
                var collections = JsonConvert.DeserializeObject<ObservableCollection<Collection>>(json);
                return collections ?? new ObservableCollection<Collection>();
            }
            catch (Exception ex)
            {
                // 在实际应用中，您可能希望记录错误或通知用户
                System.Diagnostics.Debug.WriteLine($"Error loading collections: {ex.Message}");
                return new ObservableCollection<Collection>();
            }
        }

        /// <summary>
        /// 保存单个集合到单独的文件
        /// </summary>
        public async Task SaveCollectionAsync(Collection collection, string fileName)
        {
            try
            {
                string filePath = Path.Combine(DataFolderPath, fileName);
                string json = JsonConvert.SerializeObject(collection, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving collection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从单独的文件加载集合
        /// </summary>
        public async Task<Collection> LoadCollectionAsync(string fileName)
        {
            try
            {
                string filePath = Path.Combine(DataFolderPath, fileName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string json = await File.ReadAllTextAsync(filePath);
                return JsonConvert.DeserializeObject<Collection>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading collection: {ex.Message}");
                return null;
            }
        }
    }
}

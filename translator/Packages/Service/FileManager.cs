using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace translator.Packages.Service
{
    public static class FileManager
    {
        public static List<string> GetFilesExcludingNodeModules(string rootPath, string searchPattern, List<string> excludedFiles)
        {
            var files = new List<string>();
            var directoriesToProcess = new Stack<string>();
            directoriesToProcess.Push(rootPath);

            while (directoriesToProcess.Count > 0)
            {
                var currentDirectory = directoriesToProcess.Pop();
                try
                {
                    foreach (var file in Directory.GetFiles(currentDirectory, searchPattern))
                    {
                        // Exclusion des fichiers spécifiés
                        if (!excludedFiles.Contains(Path.GetFileName(file)))
                        {
                            files.Add(file);
                        }
                    }

                    foreach (var directory in Directory.GetDirectories(currentDirectory))
                    {
                        if (Path.GetFileName(directory).Equals("node_modules", StringComparison.OrdinalIgnoreCase) || 
                            Path.GetFileName(directory).Equals(".angular", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        directoriesToProcess.Push(directory);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la lecture du répertoire {currentDirectory}: {ex.Message}");
                }
            }

            return files;
        }



        public static JObject LoadJson(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JObject.Parse(json);
        }

        public static void SaveJson(string filePath, JObject jsonObject)
        {
            var json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public static void EnsureFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var emptyJson = new JObject();
                SaveJson(filePath, emptyJson);
            }
        }

        public static JObject SortJsonKeys(JObject jsonObject)
        {
            var sortedJson = new JObject();
            foreach (var key in jsonObject.Properties().OrderBy(p => p.Name))
            {
                if (key.Value is JObject nestedObject)
                {
                    sortedJson.Add(key.Name, SortJsonKeys(nestedObject));
                }
                else if (key.Value is JArray nestedArray)
                {
                    var sortedArray = new JArray();
                    foreach (var item in nestedArray)
                    {
                        if (item is JObject nestedItemObject)
                        {
                            sortedArray.Add(SortJsonKeys(nestedItemObject));
                        }
                        else
                        {
                            sortedArray.Add(item);
                        }
                    }

                    sortedJson.Add(key.Name, sortedArray);
                }
                else
                {
                    sortedJson.Add(key.Name, key.Value);
                }
            }

            return sortedJson;
        }
    }
}
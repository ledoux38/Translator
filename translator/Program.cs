using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Translator;

public class TranslationUpdater(HttpClient client, IConfiguration configuration)
{
    private readonly string _deepLApiKey = configuration["DeepLApiKey"]
                                           ?? throw new InvalidOperationException("DeepL API key is missing.");
    private readonly string _basePath = ResolvePath(configuration["BasePath"]
                                                    ?? throw new InvalidOperationException("Base path is missing."));

    public static async Task Main()
    {
        Console.WriteLine("Chargement des configurations");
        IConfiguration configuration;
        try
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la lecture du fichier de configuration : {ex.Message}");
            return;
        }

        var httpClient = new HttpClient();
        var translationUpdater = new TranslationUpdater(httpClient, configuration);

        Console.WriteLine("Mise à jour des traductions");
        try
        {
            await translationUpdater.UpdateAllTranslations();
            Console.WriteLine("Mise à jour terminée");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la mise à jour des traductions : {ex.Message}");
        }
    }

    private static string ResolvePath(string path)
    {
        if (path.StartsWith("~/"))
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(homeDirectory,
                path.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        return path;
    }

    private async Task UpdateAllTranslations()
    {
        var frFiles = GetFilesExcludingNodeModules(_basePath, "fr.json");

        foreach (var frFile in frFiles)
        {
            var directoryPath = Path.GetDirectoryName(frFile);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                await UpdateTranslationsForDirectory(directoryPath);
            }
            else
            {
                Console.WriteLine($"Le répertoire parent de {frFile} est introuvable.");
            }
        }
    }

    private static List<string> GetFilesExcludingNodeModules(string rootPath, string searchPattern)
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
                    files.Add(file);
                }

                foreach (var directory in Directory.GetDirectories(currentDirectory))
                {
                    if (Path.GetFileName(directory).Equals("node_modules", StringComparison.OrdinalIgnoreCase))
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

    private async Task UpdateTranslationsForDirectory(string directoryPath)
    {
        string[] languages = ["en", "de", "es", "it", "nl", "pt"];
        var baseJson = LoadJson(Path.Combine(directoryPath, "fr.json"));

        foreach (var lang in languages)
        {
            var langFilePath = Path.Combine(directoryPath, $"{lang}.json");
            EnsureFileExists(langFilePath);
            var targetJson = LoadJson(langFilePath);
            await ProcessJson(baseJson, targetJson, lang);
            SaveJson(langFilePath, targetJson);
        }
    }

    public static JObject LoadJson(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JObject.Parse(json);
    }

    private static void SaveJson(string filePath, JObject jsonObject)
    {
        var json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    private static void EnsureFileExists(string filePath)
    {
        if (!File.Exists(filePath))
        {
            var emptyJson = new JObject();
            SaveJson(filePath, emptyJson);
        }
    }

    private async Task ProcessJson(JToken baseToken, JToken targetToken, string targetLanguage)
    {
        switch (baseToken.Type)
        {
            case JTokenType.Object:
                foreach (var child in baseToken.Children<JProperty>())
                {
                    var targetChild = targetToken[child.Name];
                    if (targetChild == null)
                    {
                        targetChild = new JObject();
                        ((JObject)targetToken).Add(child.Name, targetChild);
                    }

                    await ProcessJson(child.Value, targetChild, targetLanguage);
                }

                break;

            case JTokenType.Array:
                var baseArray = (JArray)baseToken;
                var targetArray = targetToken as JArray ?? new JArray();

                for (var i = 0; i < baseArray.Count; i++)
                {
                    if (targetArray.Count <= i) targetArray.Add(new JObject());
                    await ProcessJson(baseArray[i], targetArray[i], targetLanguage);
                }

                if (targetToken.Type != JTokenType.Array)
                {
                    targetToken.Replace(targetArray);
                }

                break;

            case JTokenType.String:
                if (targetToken.Type != JTokenType.String || string.IsNullOrEmpty(targetToken.ToString()))
                {
                    var translatedText = await TranslateText(baseToken.ToString(), targetLanguage);
                    targetToken.Replace(translatedText);
                }

                break;
        }
    }

    private async Task<string> TranslateText(string text, string targetLanguage)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string?>("auth_key", _deepLApiKey),
            new KeyValuePair<string, string?>("text", text),
            new KeyValuePair<string, string?>("target_lang", targetLanguage.ToUpper())
        });

        var response = await client.PostAsync("https://api-free.deepl.com/v2/translate", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var translationResult = JsonConvert.DeserializeObject<dynamic>(responseBody);
        return translationResult!.translations[0].text;
    }
}
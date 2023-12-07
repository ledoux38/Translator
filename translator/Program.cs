using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace translator;

class TranslationUpdater
{
    private static readonly HttpClient Client = new();
    private static string? _deepLApiKey;
    private static string? _basePath;

    public static async Task Main()
    {
        // Charger les configurations
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
        _deepLApiKey = configuration["DeepLApiKey"] ?? throw new InvalidOperationException();
        _basePath = configuration["BasePath"] ?? throw new InvalidOperationException();

        _basePath = ResolvePath(_basePath);
        string[] languages = { "en", "de", "es", "it", "nl", "pt" };
        var baseJson = LoadJson(Path.Combine(_basePath, "fr.json"));

        foreach (var lang in languages)
        {
            Console.WriteLine($"Ecriture dans le fichier {lang}.json");
            var targetJson = LoadJson(Path.Combine(_basePath, $"{lang}.json"));
            await ProcessJson(baseJson, targetJson, lang);
            Console.WriteLine($"Sauvegarde du fichier");
            SaveJson(Path.Combine(_basePath, $"{lang}.json"), targetJson);
        }
    }
    
    private static string ResolvePath(string path)
    {
        if (path.StartsWith("~/"))
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(homeDirectory, path.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
        return path;
    }
    

    private static JObject LoadJson(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JObject.Parse(json);
    }

    private static void SaveJson(string filePath, JObject jsonObject)
    {
        var json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    private static async Task ProcessJson(JToken baseToken, JToken targetToken, string targetLanguage)
    {
        switch (baseToken.Type)
        {
            case JTokenType.Object:
            {
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
            }
            case JTokenType.Array:
            {
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
            }
            case JTokenType.String:
            {
                if (targetToken.Type != JTokenType.String || string.IsNullOrEmpty(targetToken.ToString()))
                {
                    var translatedText = await TranslateText(baseToken.ToString(), targetLanguage);
                    targetToken.Replace(translatedText);
                }

                break;
            }
        }
    }

    private static async Task<string> TranslateText(string text, string targetLanguage)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string?>("auth_key", _deepLApiKey),
            new KeyValuePair<string, string?>("text", text),
            new KeyValuePair<string, string?>("target_lang", targetLanguage.ToUpper())
        });

        var response = await Client.PostAsync("https://api-free.deepl.com/v2/translate", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var translationResult = JsonConvert.DeserializeObject<dynamic>(responseBody);
        return translationResult!.translations[0].text;
    }
}
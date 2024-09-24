using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Translator;

public class ImportExportService(HttpClient client, IConfiguration configuration) : BaseService(client, configuration)
{
    private const string MissingTranslation = "MISSING_TRANSLATION";
    private const string ExportFileName = "translations_export.csv";
    public const string Separator = "µ";

    private const string HeaderCsv =
        $"FilePath{Separator}Key{Separator}fr{Separator}en{Separator}de{Separator}es{Separator}it{Separator}nl{Separator}pt";

    private static readonly string[] Languages = ["fr", "en", "de", "es", "it", "nl", "pt"];

    private readonly string _importExportPath = PathResolver.ResolvePath(configuration["importExportPath"]
                                                                         ?? throw new InvalidOperationException(
                                                                             "Base path is missing."));

    public async Task ExportTranslationsToCsv()
    {
        var frFiles = FileManager.GetFilesExcludingNodeModules(BasePath, "fr.json");
        var csvBuilder = new StringBuilder();

        csvBuilder.AppendLine(HeaderCsv);

        foreach (var frFile in frFiles)
        {
            var directoryPath = Path.GetDirectoryName(frFile);
            if (string.IsNullOrEmpty(directoryPath)) continue;

            var translations = new Dictionary<string, Dictionary<string, string>>();

            foreach (var lang in Languages)
            {
                var langFilePath = Path.Combine(directoryPath, $"{lang}.json");
                FileManager.EnsureFileExists(langFilePath);
                var langJson = FileManager.LoadJson(langFilePath);

                CollectTranslations(langJson, translations, lang);
            }

            foreach (var key in translations.Keys)
            {
                var line = new List<string> { frFile, key };
                line.AddRange(Languages.Select(lang => translations[key].ContainsKey(lang)
                    ? translations[key][lang].Replace("\n", "\\n")
                    : MissingTranslation));

                csvBuilder.AppendLine(string.Join(Separator, line.Select(value => $"\"{value}\"")));
            }
        }

        var outputFilePath = Path.Combine(_importExportPath, ExportFileName);
        await File.WriteAllTextAsync(outputFilePath, csvBuilder.ToString());
    }

    private void CollectTranslations(JToken token, Dictionary<string, Dictionary<string, string>> translations,
        string language, string currentKey = "")
    {
        if (token is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                var newKey = string.IsNullOrEmpty(currentKey) ? prop.Name : $"{currentKey}.{prop.Name}";
                CollectTranslations(prop.Value, translations, language, newKey);
            }
        }
        else if (token is JValue value)
        {
            if (!translations.ContainsKey(currentKey))
            {
                translations[currentKey] = new Dictionary<string, string>();
            }

            var stringValue = value.ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                translations[currentKey][language] = stringValue;
            }
            else
            {
                translations[currentKey][language] = MissingTranslation;
            }
        }
    }

    public async Task ImportTranslationsFromCsv(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            Console.WriteLine($"Le fichier CSV spécifié n'existe pas: {csvFilePath}");
            return;
        }

        var csvLines = await File.ReadAllLinesAsync(csvFilePath);
        var headers = csvLines[0].Split(Separator).Select(h => h.Trim('\"')).ToArray();

        foreach (var line in csvLines.Skip(1))
        {
            var columns = line.Split(Separator).Select(c => c.Trim('\"')).ToArray();
            var filePath = columns[Array.IndexOf(headers, "FilePath")];
            var key = columns[Array.IndexOf(headers, "Key")];

            try
            {
                var translations = new Dictionary<string, string>();
                foreach (var lang in headers.Skip(2))
                {
                    var translation = columns[Array.IndexOf(headers, lang)].Replace("\\n", "\n");
                    if (!string.IsNullOrEmpty(translation))
                    {
                        translations[lang] = translation;
                    }
                }

                await UpdateJsonFileWithTranslations(filePath, key, translations);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erreur lors de l'importation de la clé '{key}' dans le fichier '{filePath}': {ex.Message}");
            }
        }
    }

    private Task UpdateJsonFileWithTranslations(string filePath, string key,
        Dictionary<string, string> translations)
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directoryPath))
        {
            Console.WriteLine($"Répertoire introuvable pour le fichier: {filePath}");
            return Task.CompletedTask;
        }

        foreach (var lang in translations.Keys)
        {
            var langFilePath = Path.Combine(directoryPath, $"{lang}.json");
            var langJson = FileManager.LoadJson(langFilePath);

            SetJsonValue(langJson, key, translations[lang]);

            FileManager.SaveJson(langFilePath, langJson);
        }

        return Task.CompletedTask;
    }

    private static void SetJsonValue(JToken jsonToken, string keyPath, string value)
    {
        var keyParts = keyPath.Split('.');
        var currentToken = jsonToken;
        for (var i = 0; i < keyParts.Length - 1; i++)
        {
            // Si la clé existe déjà et est un JValue, remplacez-la par un JObject
            if (currentToken[keyParts[i]] is JValue)
            {
                currentToken[keyParts[i]] = new JObject();
            }

            // Si la clé n'existe pas, créez un nouveau JObject
            if (currentToken[keyParts[i]] == null)
            {
                currentToken[keyParts[i]] = new JObject();
            }

            currentToken = currentToken[keyParts[i]];
        }

        // Vérifier si la dernière clé est un JValue et la remplacer ou lancer un avertissement
        if (currentToken[keyParts.Last()] is JValue)
        {
            Console.WriteLine($"Remplacement de la clé '{keyPath}' qui était un JValue par une nouvelle valeur.");
        }

        currentToken[keyParts.Last()] = value;
    }
}
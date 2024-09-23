using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Translator;

public class ImportExportService(HttpClient client, IConfiguration configuration) : BaseService(client, configuration)
{
    private readonly string _exportPath = PathResolver.ResolvePath(configuration["exportPath"]
                                                                   ?? throw new InvalidOperationException(
                                                                       "Base path is missing."));

    private const string MissingTranslation = "MISSING_TRANSLATION";
    private const string ExportFileName = "translations_export.csv";

    public async Task ExportTranslationsToCsv()
    {
        var frFiles = FileManager.GetFilesExcludingNodeModules(BasePath, "fr.json");
        var csvBuilder = new StringBuilder();

        csvBuilder.AppendLine("File Path,Key,fr,en,de,es,it,nl,pt");

        foreach (var frFile in frFiles)
        {
            var directoryPath = Path.GetDirectoryName(frFile);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                var translations = new Dictionary<string, Dictionary<string, string>>();

                string[] languages = ["fr", "en", "de", "es", "it", "nl", "pt"];
                foreach (var lang in languages)
                {
                    var langFilePath = Path.Combine(directoryPath, $"{lang}.json");
                    FileManager.EnsureFileExists(langFilePath);
                    var langJson = FileManager.LoadJson(langFilePath);

                    CollectTranslations(langJson, translations, lang);
                }

                foreach (var key in translations.Keys)
                {
                    var line = new List<string> { frFile, key };
                    foreach (var lang in languages)
                    {
                        line.Add(translations[key].ContainsKey(lang) ? translations[key][lang] : MissingTranslation);
                    }

                    csvBuilder.AppendLine(string.Join(",", line.Select(value => $"\"{value}\"")));
                }
            }
        }

        var outputFilePath = Path.Combine(_exportPath, ExportFileName);
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
}
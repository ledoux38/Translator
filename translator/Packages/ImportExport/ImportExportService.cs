using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using translator.Packages.Service;

namespace translator.Packages.ImportExport;

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

    public async Task ExportTranslationsToCsv(List<string> filesExcluded)
    {
        var frFiles = FileManager.GetFilesExcludingNodeModules(BasePath, "fr.json", filesExcluded);
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
                var line = new List<string> { directoryPath, key };
                line.AddRange(Languages.Select(lang => translations[key].ContainsKey(lang)
                    ? translations[key][lang].Replace("\n", "\\n")
                    : MissingTranslation));

                csvBuilder.AppendLine(string.Join(Separator, line.Select(value => $"\"{value}\"")));
            }
        }

        var outputFilePath = Path.Combine(_importExportPath, ExportFileName);
        await File.WriteAllTextAsync(outputFilePath, csvBuilder.ToString());
    }


public async Task CheckMissingKeys(List<string> filesExcluded)
{
    var frFiles = FileManager.GetFilesExcludingNodeModules(BasePath, "fr.json", filesExcluded);
    var csvBuilder = new StringBuilder();
    csvBuilder.AppendLine("FilePath,Key,FR_Value,MissingLanguages");

    foreach (var frFile in frFiles)
    {
        var directoryPath = Path.GetDirectoryName(frFile);
        if (string.IsNullOrEmpty(directoryPath)) continue;
        
        var translations = new Dictionary<string, HashSet<string>>();
        var frValues = new Dictionary<string, string>();
        
        foreach (var lang in Languages)
        {
            var langFilePath = Path.Combine(directoryPath, $"{lang}.json");
            FileManager.EnsureFileExists(langFilePath);
            var langJson = FileManager.LoadJson(langFilePath);
            
            var jsonDict = (IDictionary<string, JToken?>)langJson;
            foreach (var key in jsonDict.Keys)
            {
                if (!translations.ContainsKey(key))
                {
                    translations[key] = new HashSet<string>();
                }
                translations[key].Add(lang);
                
                if (lang == "fr" && jsonDict[key] != null)
                {
                    frValues[key] = jsonDict[key].ToString();
                }
            }
        }
        
        foreach (var key in translations.Keys)
        {
            var missingLanguages = Languages.Where(lang => !translations[key].Contains(lang)).ToList();
            if (missingLanguages.Any())
            {
                var missingLangsStr = string.Join(";", missingLanguages);
                var frValue = frValues.ContainsKey(key) ? frValues[key] : "MISSING_FR";
                csvBuilder.AppendLine($"{directoryPath},{key},{frValue},{missingLangsStr}");
            }
        }
    }

    var outputFilePath = Path.Combine(_importExportPath, "missing_translations.csv");
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
            var directoryPath = columns[Array.IndexOf(headers, "FilePath")];
            var key = columns[Array.IndexOf(headers, "Key")];

            for (int i = 2; i < headers.Length; i++) // skip FilePath and Key
            {
                var lang = headers[i];
                var translation = columns[i];
                translation = translation.Replace("\\\"", "\"");

                var langFilePath = Path.Combine(directoryPath, $"{lang}.json");

                if (File.Exists(langFilePath))
                {
                    var langJson = FileManager.LoadJson(langFilePath);
                    var flattenedJson = FlattenJson(langJson);

                    if (flattenedJson.ContainsKey(key))
                    {
                        flattenedJson[key] = translation;
                    }
                    var updatedJson = JObject.FromObject(flattenedJson);
                    FileManager.SaveJson(langFilePath, updatedJson);
                }
                else
                {
                    Console.WriteLine($"Le fichier JSON pour la langue {lang} n'existe pas: {langFilePath}");
                }
            }
        }
    }

    private static Dictionary<string, string> FlattenJson(JToken token, string parentKey = "")
    {
        var result = new Dictionary<string, string>();

        switch (token)
        {
            case JObject obj:
            {
                foreach (var property in obj.Properties())
                {
                    var newKey = string.IsNullOrEmpty(parentKey) ? property.Name : $"{parentKey}.{property.Name}";
                    foreach (var kvp in FlattenJson(property.Value, newKey))
                    {
                        if (!result.ContainsKey(kvp.Key))
                        {
                            result.Add(kvp.Key, kvp.Value);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Conflit détecté pour la clé '{kvp.Key}'. Valeur existante : '{result[kvp.Key]}', Valeur en conflit : '{kvp.Value}'.(remplacement de la valeur existante)");
                            result[kvp.Key] = kvp.Value;
                        }
                    }
                }

                break;
            }
            case JValue value when !result.ContainsKey(parentKey):
                result.Add(parentKey, value.ToString(CultureInfo.InvariantCulture));
                break;
            case JValue value:
                Console.WriteLine(
                    $"Conflit détecté pour la clé '{parentKey}'. Valeur existante : '{result[parentKey]}', Valeur en conflit : '{value}'. (remplacement de la valeur existante)");
                result[parentKey] = value.ToString(CultureInfo.InvariantCulture);
                break;
        }

        return result;
    }
}
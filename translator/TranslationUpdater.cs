using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Translator
{
    public class TranslationUpdater(HttpClient client, IConfiguration configuration)
        : BaseService(client, configuration)
    {
        public async Task UpdateAllTranslations()
        {
            var frFiles = FileManager.GetFilesExcludingNodeModules(BasePath, "fr.json");

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

        private async Task UpdateTranslationsForDirectory(string directoryPath)
        {
            string[] languages = ["en", "de", "es", "it", "nl", "pt"];

            var baseJson = FileManager.LoadJson(Path.Combine(directoryPath, "fr.json"));
            FileManager.SaveJson(Path.Combine(directoryPath, "fr.json"), FileManager.SortJsonKeys(baseJson));

            foreach (var lang in languages)
            {
                var langFilePath = Path.Combine(directoryPath, $"{lang}.json");
                FileManager.EnsureFileExists(langFilePath);
                var targetJson = FileManager.LoadJson(langFilePath);
                await ProcessJson(baseJson, targetJson, lang);
                FileManager.SaveJson(langFilePath, FileManager.SortJsonKeys(targetJson));
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
                        var translatedText = await TranslationService.TranslateText(Client, DeepLApiKey,
                            baseToken.ToString(), targetLanguage);
                        targetToken.Replace(translatedText);
                    }

                    break;
            }
        }
    }
}
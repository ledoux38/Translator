using Newtonsoft.Json;

namespace Translator
{
    public static class TranslationService
    {
        public static async Task<string> TranslateText(HttpClient client, string apiKey, string text,
            string targetLanguage)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string?>("auth_key", apiKey),
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
}
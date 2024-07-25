using Microsoft.Extensions.Configuration;

namespace Translator
{
    public class Program
    {
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
    }
}
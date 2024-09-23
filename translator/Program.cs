using Microsoft.Extensions.Configuration;

namespace Translator
{
    public abstract class Program
    {
        public static async Task Main(string[] args)
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
            
            if (args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
            }
            else if (args.Contains("--translate") || args.Contains("-t"))
            {
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
            else if (args.Contains("--export") || args.Contains("-e"))
            {
                var importExportService = new ImportExportService(httpClient, configuration);
                Console.WriteLine("Exportation des traductions en CSV");
                try
                {
                    await importExportService.ExportTranslationsToCsv();
                    Console.WriteLine("Exportation terminée");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'exportation des traductions : {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(
                    "Aucun argument valide fourni. Utilisez --translate/-t pour mettre à jour les traductions ou --export/-e pour exporter les traductions en CSV.");
            }
        }
        
        private static void ShowHelp()
        {
            Console.WriteLine("Usage: translator [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -t, --translate   Mettre à jour toutes les traductions.");
            Console.WriteLine("  -e, --export      Exporter toutes les traductions en CSV.");
            Console.WriteLine("  -h, --help        Afficher l'aide.");
        }
    }
}
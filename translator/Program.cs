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
            var importExportService = new ImportExportService(httpClient, configuration);

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
            else if (args.Contains("--import") || args.Contains("-i"))
            {
                // Trouver l'index de l'argument --name ou -n
                var nameIndex = Array.FindIndex(args, a => a == "--name" || a == "-n");

                // Vérifier si un chemin de fichier suit immédiatement l'argument --name ou -n
                if (nameIndex >= 0 && nameIndex < args.Length - 1)
                {
                    var csvFilePath = args[nameIndex + 1];
                    if (File.Exists(csvFilePath))
                    {
                        Console.WriteLine($"Importation des traductions depuis le fichier CSV: {csvFilePath}");
                        try
                        {
                            await importExportService.ImportTranslationsFromCsv(csvFilePath);
                            Console.WriteLine("Importation terminée");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erreur lors de l'importation des traductions : {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Le fichier CSV spécifié n'existe pas: {csvFilePath}");
                    }
                }
                else
                {
                    Console.WriteLine(
                        "Le nom du fichier CSV est requis pour l'importation. Utilisez --name <filename>.");
                }
            }
            else if (args.Contains("--sort") || args.Contains("-s"))
            {
                Console.WriteLine("Tri des fichiers JSON");
                try
                {
                    SortAllJsonFiles(configuration);
                    Console.WriteLine("Tri terminé");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du tri des fichiers JSON : {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(
                    "Aucun argument valide fourni. Utilisez --translate/-t pour mettre à jour les traductions ou --export/-e pour exporter les traductions en CSV.");
            }
        }

        private static void SortAllJsonFiles(IConfiguration configuration)
        {
            var basePath = PathResolver.ResolvePath(configuration["BasePath"] ?? Directory.GetCurrentDirectory());
            var jsonFiles = FileManager.GetFilesExcludingNodeModules(basePath, "*.json");

            foreach (var file in jsonFiles)
            {
                var jsonObject = FileManager.LoadJson(file);
                var sortedJson = FileManager.SortJsonKeys(jsonObject);
                FileManager.SaveJson(file, sortedJson);
                Console.WriteLine($"Tri effectué pour le fichier : {file}");
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage: translator [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -t, --translate   Mettre à jour toutes les traductions.");
            Console.WriteLine("  -e, --export      Exporter toutes les traductions en CSV.");
            Console.WriteLine("  -i, --import      Importer les traductions depuis un fichier CSV.");
            Console.WriteLine("  -n, --name        Nom du fichier CSV pour l'importation.");
            Console.WriteLine("  -s, --sort        Trier toutes les clés dans les fichiers JSON.");
            Console.WriteLine("  -h, --help        Afficher l'aide.");
        }
    }
}
using Microsoft.Extensions.Configuration;
using translator.Packages.ImportExport;
using translator.Packages.Service;
using translator.Packages.Translate;

namespace translator
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
                BaseService.ShowHelp();
                return;
            }

            if (args.Contains("--translate") || args.Contains("-t"))
            {
                var translationUpdater = new TranslationUpdater(httpClient, configuration);
                Console.WriteLine("Mise à jour des traductions");
                try
                {
                    var excludedFiles = configuration["ExcludedFiles"]?.Split(',').ToList() ??
                                        Array.Empty<string>().ToList();
                    await translationUpdater.UpdateAllTranslations(excludedFiles);
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
                    var excludedFiles = configuration["ExcludedFiles"]?.Split(',').ToList() ??
                                        Array.Empty<string>().ToList();
                    await importExportService.ExportTranslationsToCsv(excludedFiles);
                    Console.WriteLine("Exportation terminée");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'exportation des traductions : {ex.Message}");
                }
            }
            else if (args.Contains("--compare") || args.Contains("-c"))
            {
                Console.WriteLine("compare file");
                try
                {
                    var excludedFiles = configuration["ExcludedFiles"]?.Split(',').ToList() ??
                                        Array.Empty<string>().ToList();
                    await importExportService.CheckMissingKeys(excludedFiles);
                    Console.WriteLine("compare complited");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la comparaison des traductions : {ex.Message}");
                }
            }
            
            else if (args.Contains("--import") || args.Contains("-i"))
            {
                var nameIndex = Array.FindIndex(args, a => a == "--name" || a == "-n");

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
                    BaseService.SortAllJsonFiles(configuration);
                    Console.WriteLine("Tri terminé");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du tri des fichiers JSON : {ex.Message}");
                }
            }
            else if (args.Contains("--flatten") || args.Contains("-f"))
            {
                Console.WriteLine("Aplatissement des fichiers JSON");
                try
                {
                    BaseService.FlattenAllJsonFiles(configuration);
                    Console.WriteLine("Aplatissement terminé");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'aplatissement des fichiers JSON : {ex.Message}");
                }
            }
            else if (args.Contains("--unflatten") || args.Contains("-u"))
            {
                Console.WriteLine("Imbrication des clés des fichiers JSON");
                try
                {
                    BaseService.UnflattenAllJsonFiles(configuration);
                    Console.WriteLine("Imbrication terminée");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'imbrication des fichiers JSON : {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(
                    "Aucun argument valide fourni.");
                BaseService.ShowHelp();
            }
        }
    }
}
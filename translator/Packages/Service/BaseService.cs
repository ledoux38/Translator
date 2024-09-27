using Microsoft.Extensions.Configuration;
using translator.Packages.ManageJson;

namespace translator.Packages.Service;

public class BaseService
{
    protected readonly HttpClient Client;
    protected readonly string DeepLApiKey;
    protected readonly string BasePath;

    protected BaseService(HttpClient client, IConfiguration configuration)
    {
        Client = client;
        DeepLApiKey = configuration["DeepLApiKey"]
                      ?? throw new InvalidOperationException("DeepL API key is missing.");
        BasePath = PathResolver.ResolvePath(configuration["BasePath"]
                                            ?? throw new InvalidOperationException("Base path is missing."));
    }

    public static void UnflattenAllJsonFiles(IConfiguration configuration)
    {
        var basePath = PathResolver.ResolvePath(configuration["BasePath"] ?? Directory.GetCurrentDirectory());
        var excludedFiles = configuration["ExcludedFiles"]?.Split(',').ToList() ?? Array.Empty<string>().ToList();
        var jsonFiles = FileManager.GetFilesExcludingNodeModules(basePath, "*.json", excludedFiles);

        foreach (var file in jsonFiles)
        {
            try
            {
                var flatJson = FileManager.LoadJson(file);
                var unflattenedJson = JsonUnflattener.UnflattenJson(flatJson);
                FileManager.SaveJson(file, unflattenedJson);
                Console.WriteLine($"Imbrication effectuée pour le fichier : {file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'imbrication des fichiers JSON : {ex.Message}");
            }
        }
    }

    public static void FlattenAllJsonFiles(IConfiguration configuration)
    {
        var basePath = PathResolver.ResolvePath(configuration["BasePath"] ?? Directory.GetCurrentDirectory());
        var excludedFiles = configuration["ExcludedFiles"]?.Split(',').ToList() ?? Array.Empty<string>().ToList();
        var jsonFiles = FileManager.GetFilesExcludingNodeModules(basePath, "*.json", excludedFiles);

        foreach (var file in jsonFiles)
        {
            try
            {
                var jsonObject = FileManager.LoadJson(file);
                var flattenedJson = JsonFlattener.FlattenJson(jsonObject);
                FileManager.SaveJson(file, flattenedJson);
                Console.WriteLine($"Aplatissement effectué pour le fichier : {file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'aplatissement des fichiers JSON : {ex.Message}");
            }
        }
    }

    public static void SortAllJsonFiles(IConfiguration configuration)
    {
        var basePath = PathResolver.ResolvePath(configuration["BasePath"] ?? Directory.GetCurrentDirectory());
        var excludedFiles = configuration["ExcludedFiles"]?.Split(',').ToList() ?? Array.Empty<string>().ToList();
        var jsonFiles = FileManager.GetFilesExcludingNodeModules(basePath, "*.json", excludedFiles);


        foreach (var file in jsonFiles)
        {
            try
            {
                var jsonObject = FileManager.LoadJson(file);
                var sortedJson = FileManager.SortJsonKeys(jsonObject);
                FileManager.SaveJson(file, sortedJson);
                Console.WriteLine($"Tri effectué pour le fichier : {file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du tri des fichiers JSON : {ex.Message}");
            }
        }
    }

    public static void ShowHelp()
    {
        Console.WriteLine("Usage: translator [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -t, --translate   Mettre à jour toutes les traductions.");
        Console.WriteLine("  -e, --export      Exporter toutes les traductions en CSV.");
        Console.WriteLine("  -i, --import      Importer les traductions depuis un fichier CSV.");
        Console.WriteLine("  -n, --name        Nom du fichier CSV pour l'importation.");
        Console.WriteLine("  -s, --sort        Trier toutes les clés dans les fichiers JSON.");
        Console.WriteLine("  -f, --flatten     Aplatir toutes les clés dans les fichiers JSON.");
        Console.WriteLine(
            "  -u, --unflatten   Réorganiser toutes les clés aplaties en structure imbriquée dans les fichiers JSON.");
        Console.WriteLine("  -h, --help        Afficher l'aide.");
    }
}
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using translator.Packages;
using translator.Packages.ImportExport;
using translator.Packages.Service;

namespace unittest;

[TestFixture]
public class TranslationUpdaterTests
{
    private const string TestDirectory = "testData";
    private const string FrJsonFileName = "fr.json";
    private const string EnJsonFileName = "en.json";
    private const string TestExportCsvPath = "testData/translations_export.csv";
    private const string TestImportCsvPath = "testData/translations_import.csv";
    private readonly ImportExportService _importExportService;

    public TranslationUpdaterTests()
    {
        var config = new Dictionary<string, string>
        {
            { "importExportPath", TestDirectory },
            { "BasePath", TestDirectory },
            { "DeepLApiKey", "fake-api-key-for-testing" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(config!).Build();
        _importExportService = new ImportExportService(new HttpClient(), configuration);
    }

    [SetUp]
    public void Setup()
    {
        if (!Directory.Exists(TestDirectory))
        {
            Directory.CreateDirectory(TestDirectory);
        }

        CreateTestJsonFile(FrJsonFileName, new JObject
        {
            ["greeting"] = "Bonjour",
            ["farewell"] = "Au revoir"
        });
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(TestDirectory))
        {
            Directory.Delete(TestDirectory, true);
        }
    }

    [Test]
    public async Task ExportTranslationsToCsv_ShouldCreateCsvWithTranslations()
    {
        CreateTestJsonFile(EnJsonFileName, new JObject
        {
            ["greeting"] = "Hello",
            ["farewell"] = "Goodbye"
        });


        await _importExportService.ExportTranslationsToCsv(new List<string>());


        Assert.IsTrue(File.Exists(TestExportCsvPath));

        var csvContent = await File.ReadAllTextAsync(TestExportCsvPath);
        StringAssert.Contains("greeting", csvContent);
        StringAssert.Contains("Bonjour", csvContent);
        StringAssert.Contains("Hello", csvContent);
        StringAssert.Contains("farewell", csvContent);
        StringAssert.Contains("Au revoir", csvContent);
        StringAssert.Contains("Goodbye", csvContent);
    }

    [Test]
    public async Task ImportTranslationsFromCsv_ShouldUpdateJsonFiles()
    {
        CreateTestJsonFile(EnJsonFileName, new JObject());

        var csvContent =
            $"FilePath{ImportExportService.Separator}Key{ImportExportService.Separator}fr{ImportExportService.Separator}en\n" +
            $"{TestDirectory}/{FrJsonFileName}{ImportExportService.Separator}greeting{ImportExportService.Separator}Salut{ImportExportService.Separator}\n" +
            $"{TestDirectory}/{EnJsonFileName}{ImportExportService.Separator}greeting{ImportExportService.Separator}{ImportExportService.Separator}Hi\n";
        await File.WriteAllTextAsync(TestImportCsvPath, csvContent);


        await _importExportService.ImportTranslationsFromCsv(TestImportCsvPath);


        var frJson = JObject.Parse(await File.ReadAllTextAsync(Path.Combine(TestDirectory, FrJsonFileName)));
        var enJson = JObject.Parse(await File.ReadAllTextAsync(Path.Combine(TestDirectory, EnJsonFileName)));

        Assert.That(frJson["greeting"]?.ToString(), Is.EqualTo("Salut"));
        Assert.That(enJson["greeting"]?.ToString(), Is.EqualTo("Hi"));
    }

    [Test]
    public void LoadJson_ValidFile_ShouldReturnJObject()
    {
        var jsonContent = FileManager.LoadJson(Path.Combine(TestDirectory, FrJsonFileName));
        Assert.IsNotNull(jsonContent);
        Assert.That(jsonContent["greeting"]?.ToString(), Is.EqualTo("Bonjour"));
    }

    [Test]
    public void SaveJson_ShouldWriteToFile()
    {
        var targetJson = new JObject
        {
            ["exampleKey"] = "exampleValue"
        };

        var filePath = Path.Combine(TestDirectory, EnJsonFileName);
        FileManager.SaveJson(filePath, targetJson);

        Assert.IsTrue(File.Exists(filePath));

        var savedJson = JObject.Parse(File.ReadAllText(filePath));
        Assert.That(savedJson["exampleKey"]?.ToString(), Is.EqualTo("exampleValue"));
    }

    [Test]
    public void EnsureFileExists_ShouldCreateFileIfNotExists()
    {
        var filePath = Path.Combine(TestDirectory, "testEnsureFileExists.json");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        FileManager.EnsureFileExists(filePath);

        Assert.IsTrue(File.Exists(filePath));

        var jsonContent = JObject.Parse(File.ReadAllText(filePath));
        Assert.IsNotNull(jsonContent);
        Assert.IsFalse(jsonContent.HasValues);

        File.Delete(filePath);
    }

    [Test]
    public void ResolvePath_ShouldReturnCorrectPath()
    {
        var relativePath = "~/testData";
        var resolvedPath = PathResolver.ResolvePath(relativePath);

        var expectedPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "testData");
        Assert.That(resolvedPath, Is.EqualTo(expectedPath));
    }

    [Test]
    public void SortJsonKeys_ShouldSortKeysAlphabetically()
    {
        var jsonObject = new JObject
        {
            ["zKey"] = "zValue",
            ["aKey"] = "aValue",
            ["mKey"] = new JObject
            {
                ["bKey"] = "bValue",
                ["aKey"] = "aValue"
            },
            ["kKey"] = new JArray(
                new JObject
                {
                    ["dKey"] = "dValue",
                    ["cKey"] = "cValue"
                }
            )
        };

        var sortedJson = FileManager.SortJsonKeys(jsonObject);

        var expectedJson = new JObject
        {
            ["aKey"] = "aValue",
            ["kKey"] = new JArray(
                new JObject
                {
                    ["cKey"] = "cValue",
                    ["dKey"] = "dValue"
                }
            ),
            ["mKey"] = new JObject
            {
                ["aKey"] = "aValue",
                ["bKey"] = "bValue"
            },
            ["zKey"] = "zValue"
        };

        Assert.That(sortedJson.ToString(), Is.EqualTo(expectedJson.ToString()));
    }

    private static void CreateTestJsonFile(string fileName, JObject content)
    {
        var filePath = Path.Combine(TestDirectory, fileName);
        File.WriteAllText(filePath, content.ToString());
    }
}
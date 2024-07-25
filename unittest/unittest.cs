using Newtonsoft.Json.Linq;
using Translator;

namespace unittest
{
    [TestFixture]
    public class TranslationUpdaterTests
    {
        [Test]
        public void LoadJson_ValidFile_ShouldReturnJObject()
        {
            var jsonContent = FileManager.LoadJson("testData/fr.json");
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
            
            var filePath = "testData/en.json";
            
            FileManager.SaveJson(filePath, targetJson);
            
            Assert.IsTrue(File.Exists(filePath));
            
            var savedJson = JObject.Parse(File.ReadAllText(filePath));
            Assert.That(savedJson["exampleKey"]?.ToString(), Is.EqualTo("exampleValue"));
            
            File.Delete(filePath);
        }

        [Test]
        public void EnsureFileExists_ShouldCreateFileIfNotExists()
        {
            var filePath = "testData/testEnsureFileExists.json";
            
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
    }
}
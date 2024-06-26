using Newtonsoft.Json.Linq;
using Translator;
using static NUnit.Framework.Assert;

namespace unittest;

[TestFixture]
public class TranslationUpdaterTests
{
    [Test]
    public void LoadJson_ValidFile_ShouldReturnJObject()
    {
        var jsonContent = TranslationUpdater.LoadJson("testData/fr.json");
        IsNotNull(jsonContent);
        That(jsonContent["greeting"]?.ToString(), Is.EqualTo("Bonjour"));
    }
    
    // [Test]
    // public void SaveJson_ShouldWriteToFile()
    // {
    //     // Création de l'objet JSON fictif
    //     var targetJson = new JObject
    //     {
    //         ["exampleKey"] = "exampleValue"
    //     };
    //
    //     // Appel de la méthode SaveJson
    //     TranslationUpdater.SaveJson( "en.json", targetJson);
    //
    //     // Vérifier que le fichier a été créé
    //     Assert.IsTrue(File.Exists( "en.json"));
    //
    //     // Vérifier le contenu du fichier
    //     var savedJson = JObject.Parse(File.ReadAllText("en.json"));
    //     Assert.AreEqual("exampleValue", savedJson["exampleKey"].ToString());
    // }

}
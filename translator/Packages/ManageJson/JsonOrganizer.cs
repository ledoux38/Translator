using Newtonsoft.Json.Linq;

namespace translator.Packages.ManageJson;

public static class JsonOrganizer
{
    public static JObject OrganizeJson(JObject jsonObject)
    {
        var organizedObject = new JObject();

        foreach (var property in jsonObject.Properties())
        {
            AddNestedProperty(organizedObject, property.Name, property.Value);
        }

        return organizedObject;
    }

    private static void AddNestedProperty(JObject parentObject, string key, JToken value)
    {
        var keyParts = key.Split('.');

        for (int i = 0; i < keyParts.Length - 1; i++)
        {
            var currentKey = keyParts[i];

            if (parentObject[currentKey] == null)
            {
                parentObject[currentKey] = new JObject();
            }
            else if (parentObject[currentKey] is not JObject)
            {
                parentObject[currentKey] = new JObject();
            }

            parentObject = (JObject)parentObject[keyParts[i]]!;
        }

        var lastKey = keyParts.Last();
        if (parentObject[lastKey] is JObject existingObject && value is JObject newObjectToMerge)
        {
            existingObject.Merge(newObjectToMerge, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            });
        }
        else
        {
            parentObject[lastKey] = value;
        }
    }
}
using Newtonsoft.Json.Linq;

namespace translator.Packages.ManageJson;

public static class JsonUnflattener
{
    public static JObject UnflattenJson(JObject flatJson)
    {
        var unflattenedObject = new JObject();

        foreach (var property in flatJson.Properties())
        {
            AddNestedProperty(unflattenedObject, property.Name, property.Value);
        }

        return unflattenedObject;
    }

    private static void AddNestedProperty(JObject parentObject, string flatKey, JToken value)
    {
        var keyParts = flatKey.Split('.');
        JObject currentObject = parentObject;

        for (int i = 0; i < keyParts.Length; i++)
        {
            var keyPart = keyParts[i];

            if (keyPart.Contains("[") && keyPart.Contains("]"))
            {
                // Handle array notation
                var arrayKey = keyPart.Substring(0, keyPart.IndexOf('['));
                var index = int.Parse(keyPart.Substring(keyPart.IndexOf('[') + 1,
                    keyPart.IndexOf(']') - keyPart.IndexOf('[') - 1));

                if (!(currentObject[arrayKey] is JArray array))
                {
                    array = new JArray();
                    currentObject[arrayKey] = array;
                }

                while (array.Count <= index)
                {
                    array.Add(null!);
                }

                if (i == keyParts.Length - 1)
                {
                    array[index] = value;
                }
                else
                {
                    if (array[index] is not JObject)
                    {
                        array[index] = new JObject();
                    }

                    currentObject = (JObject)array[index];
                }
            }
            else
            {
                if (i == keyParts.Length - 1)
                {
                    currentObject[keyPart] = value;
                }
                else
                {
                    if (!(currentObject[keyPart] is JObject nestedObject))
                    {
                        nestedObject = new JObject();
                        currentObject[keyPart] = nestedObject;
                    }

                    currentObject = nestedObject;
                }
            }
        }
    }
}
using Newtonsoft.Json.Linq;

namespace translator.Packages.ManageJson;

public static class JsonFlattener
{
    public static JObject FlattenJson(JObject jsonObject)
    {
        var flattenedObject = new JObject();
        var keyTracker = new HashSet<string>(); // Utiliser un HashSet pour suivre les clés uniques
        FlattenJsonRecursively(jsonObject, flattenedObject, keyTracker, null);
        return flattenedObject;
    }

    private static void FlattenJsonRecursively(JToken token, JObject flattenedObject, HashSet<string> keyTracker, string prefix)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties())
            {
                var newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                FlattenJsonRecursively(property.Value, flattenedObject, keyTracker, newPrefix);
            }
        }
        else if (token is JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                var newPrefix = $"{prefix}[{i}]";
                FlattenJsonRecursively(array[i], flattenedObject, keyTracker, newPrefix);
            }
        }
        else
        {
            // Si le token est un JValue (chaîne, nombre, etc.), l'ajouter à l'objet aplati
            if (keyTracker.Contains(prefix))
            {
                Console.WriteLine($"Clé en double détectée : {prefix}");
            }
            else
            {
                keyTracker.Add(prefix);
                flattenedObject[prefix] = token;
            }
        }
    }
}
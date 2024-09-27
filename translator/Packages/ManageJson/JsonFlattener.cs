using Newtonsoft.Json.Linq;

namespace translator.Packages.ManageJson;

public static class JsonFlattener
{
    public static JObject FlattenJson(JObject jsonObject)
    {
        var flattenedObject = new JObject();
        var keyTracker = new HashSet<string>();
        FlattenJsonRecursively(jsonObject, flattenedObject, keyTracker);
        return flattenedObject;
    }

    private static void FlattenJsonRecursively(JToken token, JObject flattenedObject, HashSet<string> keyTracker,
        string? prefix = null)
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
            for (var i = 0; i < array.Count; i++)
            {
                var newPrefix = $"{prefix}[{i}]";
                FlattenJsonRecursively(array[i], flattenedObject, keyTracker, newPrefix);
            }
        }
        else
        {
            if (prefix != null && keyTracker.Contains(prefix))
            {
                Console.WriteLine($"Clé en double détectée : {prefix}");
            }
            else
            {
                if (prefix == null) return;
                keyTracker.Add(prefix);
                flattenedObject[prefix] = token;
            }
        }
    }
}
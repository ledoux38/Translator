using Microsoft.Extensions.Configuration;

namespace Translator;

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
}
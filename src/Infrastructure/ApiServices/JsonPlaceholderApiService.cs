
using Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Infrastructure.ApiServices;
public class JsonPlaceholderApiService 
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<IntegrationOptions> _options;

    public JsonPlaceholderApiService(HttpClient httpClient, IOptions<IntegrationOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
        _httpClient.BaseAddress = new Uri(_options.Value.JsonPlaceholderApiDomain);
    }
    
}

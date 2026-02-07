using System.Net.Http.Json;
using System.Text.Json;

namespace IntegrationTests.Fixtures;

/// <summary>
/// Cliente HTTP wrapper que encapsula as opções de serialização JSON.
/// 
/// Problema que resolve:
/// O HttpClient padrão não serializa enums como strings, causando erros
/// de desserialização nos testes. Esta classe garante que todas as
/// operações usem as mesmas JsonSerializerOptions configuradas.
/// 
/// Uso:
/// <code>
/// var response = await _client.PostAsJsonAsync("/api/orders", request);
/// var order = await _client.ReadContentAsJsonAsync&lt;Order&gt;(response.Content);
/// </code>
/// </summary>
public class ApiClient
{
    /// <summary>
    /// HttpClient interno. Use apenas se precisar de acesso direto
    /// para operações não cobertas por esta classe.
    /// </summary>
    public HttpClient HttpClient { get; }
    
    private readonly JsonSerializerOptions _jsonOptions;
    
    public ApiClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
    {
        HttpClient = httpClient;
        _jsonOptions = jsonOptions;
    }

    /// <summary>
    /// Envia uma requisição POST com o corpo serializado em JSON.
    /// Usa as opções de serialização configuradas (ex: enums como strings).
    /// </summary>
    public Task<HttpResponseMessage> PostAsJsonAsync<T>(string uri, T value)
    {
        return HttpClient.PostAsJsonAsync(uri, value, _jsonOptions);
    }
    
    /// <summary>
    /// Lê o conteúdo da resposta HTTP e desserializa para o tipo especificado.
    /// Usa as opções de serialização configuradas (ex: enums como strings).
    /// </summary>
    public Task<T?> ReadContentAsJsonAsync<T>(HttpContent content)
    {
        return content.ReadFromJsonAsync<T>(_jsonOptions);
    }
    
    /// <summary>
    /// Envia uma requisição GET e desserializa a resposta para o tipo especificado.
    /// </summary>
    public Task<T?> GetFromJsonAsync<T>(string uri)
    {
        return HttpClient.GetFromJsonAsync<T>(uri, _jsonOptions);
    }
    
    /// <summary>
    /// Envia uma requisição PUT com o corpo serializado em JSON.
    /// </summary>
    public Task<HttpResponseMessage> PutAsJsonAsync<T>(string uri, T value)
    {
        return HttpClient.PutAsJsonAsync(uri, value, _jsonOptions);
    }
    
    /// <summary>
    /// Envia uma requisição DELETE.
    /// </summary>
    public Task<HttpResponseMessage> DeleteAsync(string uri)
    {
        return HttpClient.DeleteAsync(uri);
    }
}

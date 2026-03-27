using System.Net.Http.Json;
using System.Text.Json;
using AwesomeFiles.Client.Models;

namespace AwesomeFiles.Client.Api;

public sealed class AwesomeFilesApiClient
{
    private readonly HttpClient _httpClient;

    public AwesomeFilesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<string>> GetFilesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/files", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var files = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<string>>(cancellationToken: cancellationToken);
        return files ?? Array.Empty<string>();
    }

    public async Task<long> CreateArchiveAsync(IReadOnlyCollection<string> fileNames, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/archives", new CreateArchiveRequest
        {
            FileNames = fileNames
        }, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<CreateArchiveResponse>(cancellationToken: cancellationToken)
                      ?? throw new InvalidOperationException("Backend returned empty response.");

        return payload.TaskId;
    }

    public async Task<ArchiveStatusResponse> GetStatusAsync(long taskId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/archives/{taskId}/status", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<ArchiveStatusResponse>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("Backend returned empty response.");
    }

    public async Task<string> DownloadArchiveAsync(long taskId, string targetFolder, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(targetFolder);

        var response = await _httpClient.GetAsync($"api/archives/{taskId}/download", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var outputPath = Path.Combine(targetFolder, $"{taskId}.zip");

        await using var outputStream = File.Create(outputPath);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await responseStream.CopyToAsync(outputStream, cancellationToken);

        return outputPath;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException($"Backend request failed with status code {(int)response.StatusCode}.");
        }

        try
        {
            using var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            if (root.TryGetProperty("detail", out var detail))
            {
                throw new InvalidOperationException(detail.GetString() ?? content);
            }

            if (root.TryGetProperty("title", out var title))
            {
                throw new InvalidOperationException(title.GetString() ?? content);
            }
        }
        catch (JsonException)
        {
        }

        throw new InvalidOperationException(content);
    }
}

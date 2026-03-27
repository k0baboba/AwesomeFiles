using AwesomeFiles.Client.Api;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5272";

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(baseUrl)
};

var apiClient = new AwesomeFilesApiClient(httpClient);

Console.WriteLine("AwesomeFiles client was started.");
Console.WriteLine($"Backend URL: {httpClient.BaseAddress}");
Console.WriteLine("Type 'help' to see all commands.");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var command = parts[0].ToLowerInvariant();

    try
    {
        switch (command)
        {
            case "help":
                PrintHelp();
                break;

            case "list":
                var files = await apiClient.GetFilesAsync();
                Console.WriteLine(files.Count == 0
                    ? "No files are available."
                    : string.Join(' ', files));
                break;

            case "create-archive":
                if (parts.Length < 2)
                {
                    Console.WriteLine("Usage: create-archive <file1> <file2> ...");
                    break;
                }

                var taskId = await apiClient.CreateArchiveAsync(parts.Skip(1).ToArray());
                Console.WriteLine($"Create archive task is started, id: {taskId}");
                break;

            case "status":
                if (parts.Length != 2 || !long.TryParse(parts[1], out var statusTaskId))
                {
                    Console.WriteLine("Usage: status <taskId>");
                    break;
                }

                var status = await apiClient.GetStatusAsync(statusTaskId);
                Console.WriteLine($"Task {status.TaskId}: {status.Status}");

                if (!string.IsNullOrWhiteSpace(status.Error))
                {
                    Console.WriteLine($"Error: {status.Error}");
                }

                break;

            case "download":
                if (parts.Length != 3 || !long.TryParse(parts[1], out var downloadTaskId))
                {
                    Console.WriteLine("Usage: download <taskId> <targetFolder>");
                    break;
                }

                var filePath = await apiClient.DownloadArchiveAsync(downloadTaskId, parts[2]);
                Console.WriteLine($"Archive downloaded: {filePath}");
                break;

            case "exit":
                return;

            default:
                Console.WriteLine("Unknown command. Type 'help' for commands list.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static void PrintHelp()
{
    Console.WriteLine("Commands:");
    Console.WriteLine("  list");
    Console.WriteLine("  create-archive <file1> <file2> ...");
    Console.WriteLine("  status <taskId>");
    Console.WriteLine("  download <taskId> <targetFolder>");
    Console.WriteLine("  exit");
}

// dotnet new console -n RunwayPoc
// dotnet add package System.Net.Http.Json

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
namespace ConsolePoC;

internal class Program
{
    private static string _promptScene = "Anime cinematic style, 1985 apartment at night. A dimly lit living room with warm yellow lamp light. A rotary telephone sits on a wooden table. Suddenly the phone rings loudly. A man in his early 30s freezes mid-step, staring at the phone with tension and uncertainty. Slow camera push-in. Subtle dramatic lighting. Film grain texture. Realistic shadows. 16:9.";
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        await RunWayPock();
    }

    private static async Task RunWayPock()
    {
        var apiKey = Environment.GetEnvironmentVariable("RUNWAYML_API_SECRET",EnvironmentVariableTarget.Machine)
           ?? throw new Exception("Set RUNWAYML_API_SECRET env var.");

        var client = new HttpClient { BaseAddress = new Uri("https://api.dev.runwayml.com/") };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Add("X-Runway-Version", "2024-11-06");

        var createBody = new
        {
            model = "gen4.5",
            //model = "gen3a_turbo",
            promptText = _promptScene ?? "Anime style, office training scenario. A manager listens calmly while an employee reports a conflict. Medium shot. Clean background. Subtle motion.",
            ratio = "1280:720",
            duration = 5
        };

        var createResp = await client.PostAsJsonAsync("v1/text_to_video", createBody);
        //createResp.EnsureSuccessStatusCode();
        if (!createResp.IsSuccessStatusCode)
        {
            var error = await createResp.Content.ReadAsStringAsync();
            Console.WriteLine($"Create failed: {createResp.StatusCode}");
            Console.WriteLine(error);
            return;
        }

        var createJson = await createResp.Content.ReadAsStringAsync();
        var taskId = JsonDocument.Parse(createJson).RootElement.GetProperty("id").GetString();
        Console.WriteLine($"TaskId: {taskId}");

        string? outputUrl = null;

        int attempts = 0;
        const int maxAttempts = 60; // 5 minutes total

        while (attempts < maxAttempts)
        {
            attempts++;    
            await Task.Delay(TimeSpan.FromSeconds(5)); // respect Runway guidance
            var taskResp = await client.GetAsync($"v1/tasks/{taskId}");
            taskResp.EnsureSuccessStatusCode();

            var taskJson = await taskResp.Content.ReadAsStringAsync();
            //var root = JsonDocument.Parse(taskJson).RootElement;
            using var doc = JsonDocument.Parse(taskJson);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();
            Console.WriteLine($"Status: {status}");

            if (status == "SUCCEEDED")
            {
                outputUrl = root.GetProperty("output")[0].GetString();
                break;
            }

            if (status == "FAILED")
                throw new Exception($"Runway task failed: {taskJson}");
        }
        if (attempts >= maxAttempts)
        {
            throw new TimeoutException("Runway task polling timed out.");
        }

        Console.WriteLine($"Output URL: {outputUrl}");

        using var mp4Stream = await client.GetStreamAsync(outputUrl);
        await using var file = File.Create("output.mp4");
        await mp4Stream.CopyToAsync(file);

        Console.WriteLine("Saved: output.mp4");
    }
}

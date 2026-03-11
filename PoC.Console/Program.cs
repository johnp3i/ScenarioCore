// dotnet new console -n RunwayPoc
// dotnet add package System.Net.Http.Json

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PoC;

internal class Program
{

    static void Main(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("RUNWAYML_API_SECRET")
           ?? throw new Exception("Set RUNWAYML_API_SECRET env var.");

        Console.WriteLine("Hello, World!");
    }
}

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNetEnv;

class Program
{
    static async Task Main()
    {
        Env.Load();

        string? endpoint = Environment.GetEnvironmentVariable("AZURE_ML_ENDPOINT");
        string? apiKey = Environment.GetEnvironmentVariable("AZURE_ML_API_KEY");

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Console.WriteLine("AZURE_ML_ENDPOINT saknas i .env");
            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("AZURE_ML_API_KEY saknas i .env");
            return;
        }

        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        Console.Write("Ange motorvolym i liter: ");

        if (!double.TryParse(
                Console.ReadLine(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double engineVolume))
        {
            Console.WriteLine("Felaktigt värde.");
            return;
        }

        string requestBody = $$"""
        {
            "Inputs": {
                "input1": [
                    {
                        "EngineVolumeLitres": {{engineVolume}}
                    }
                ]
            },
            "GlobalParameters": {}
        }
        """;

        using var content = new StringContent(
            requestBody,
            Encoding.UTF8,
            "application/json");

        try
        {
            Console.WriteLine();
            Console.WriteLine("Skickar data till Azure ML...");

            HttpResponseMessage response =
                await client.PostAsync(endpoint, content);

            string result =
                await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using JsonDocument json =
                    JsonDocument.Parse(result);

                double prediction =
                    json.RootElement
                        .GetProperty("Results")
                        .GetProperty("WebServiceOutput0")[0]
                        .GetProperty("Scored Labels")
                        .GetDouble();

                Console.WriteLine();
                Console.WriteLine("Prediction från Azure ML:");
                Console.WriteLine(
                    $"Motorvolym: {engineVolume:F2} liter");

                Console.WriteLine(
                    $"Förutsagd bränsleförbrukning: {prediction:F2} L/100 km");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Request misslyckades.");
                Console.WriteLine($"Statuskod: {(int)response.StatusCode}");
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine();
                Console.WriteLine(result);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine();
            Console.WriteLine("Kunde inte ansluta till Azure ML.");
            Console.WriteLine(ex.Message);
        }
        catch (JsonException ex)
        {
            Console.WriteLine();
            Console.WriteLine("Kunde inte läsa Azure ML:s svar.");
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Ett fel uppstod.");
            Console.WriteLine(ex.Message);
        }
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.GenAI;
using Google.GenAI.Types;

namespace SmartHealthcare.API.AI.LLM;

public class LLMService : ILLMService
{
    private readonly Client _client;
    private readonly string _model;

    private const int MaxAttempts = 3;

    public LLMService(IConfiguration configuration)
    {
        string? apiKey = configuration["Gemini:ApiKey"];

        _model =
            configuration["Gemini:Model"]
            ?? "gemini-flash-latest";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured.");
        }

        _client = new Client(apiKey: apiKey);
    }

    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt)
    {
        GenerateContentConfig config = new()
        {
            SystemInstruction = new Content
            {
                Parts =
                [
                    new Part
                    {
                        Text = systemPrompt
                    }
                ]
            },
            Temperature = 0.1
        };

        var response = await ExecuteWithRetryAsync(
            () => _client.Models.GenerateContentAsync(
                model: _model,
                contents: userPrompt,
                config: config));

        string? responseText =
            response.Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .FirstOrDefault()?
                .Text;

        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException(
                "Gemini returned an empty response.");
        }

        return responseText;
    }

    public async Task<T> GenerateStructuredAsync<T>(
        string systemPrompt,
        string userPrompt,
        string jsonSchema)
    {
        GenerateContentConfig config = new()
        {
            SystemInstruction = new Content
            {
                Parts =
                [
                    new Part
                    {
                        Text = systemPrompt
                    }
                ]
            },

            ResponseMimeType = "application/json",
            ResponseJsonSchema = JsonNode.Parse(jsonSchema),
            Temperature = 0.1
        };

        var response = await ExecuteWithRetryAsync(
            () => _client.Models.GenerateContentAsync(
                model: _model,
                contents: userPrompt,
                config: config));

        string? responseText =
            response.Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .FirstOrDefault()?
                .Text;

        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException(
                "Gemini returned an empty structured response.");
        }

        T? result = JsonSerializer.Deserialize<T>(
            responseText,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result == null)
        {
            throw new InvalidOperationException(
                "Gemini response could not be converted to the requested type.");
        }

        return result;
    }

    private static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (ServerError) when (attempt < MaxAttempts)
            {
                // Wait 1 second before attempt 2,
                // then 2 seconds before attempt 3.
                int delayMilliseconds =
                    1000 * (int)Math.Pow(2, attempt - 1);

                await Task.Delay(delayMilliseconds);
            }
        }

        // This line is only a defensive fallback.
        throw new InvalidOperationException(
            "Gemini request failed after all retry attempts.");
    }
}
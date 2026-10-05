 namespace SmartHealthcare.API.AI.LLM;

public interface ILLMService
{
    Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt);

    Task<T> GenerateStructuredAsync<T>(
        string systemPrompt,
        string userPrompt,
        string jsonSchema);
}
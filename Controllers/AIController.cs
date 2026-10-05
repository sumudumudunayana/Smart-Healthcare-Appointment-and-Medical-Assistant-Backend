using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.AI.LLM;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly ILLMService _llmService;

    public AIController(
        ILLMService llmService)
    {
        _llmService = llmService;
    }

    [HttpPost("test")]
    public async Task<ActionResult<object>> Test(
        [FromBody] string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return BadRequest(new
            {
                message = "Prompt is required."
            });
        }

        string response =
            await _llmService.GenerateAsync(
                "You are a helpful AI assistant for a smart healthcare system.",
                prompt);

        return Ok(new
        {
            success = true,
            response
        });
    }
}
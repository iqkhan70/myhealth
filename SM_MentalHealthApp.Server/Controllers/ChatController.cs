using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("send")]
        public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                Console.WriteLine($"ChatController.SendMessage called with prompt: '{request.Prompt}'");
                
                var response = await _chatService.SendMessageAsync(
                    request.Prompt, 
                    request.ConversationId, 
                    request.Provider,
                    request.PatientId);
                
                Console.WriteLine($"ChatController returning response: '{response.Message}'");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChatController error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("patient/{patientId}/send")]
        public async Task<ActionResult<ChatResponse>> SendMessageForPatient(int patientId, [FromBody] ChatRequest request)
        {
            try
            {
                Console.WriteLine($"ChatController.SendMessageForPatient called with prompt: '{request.Prompt}' for patient: {patientId}");
                
                var response = await _chatService.SendMessageAsync(
                    request.Prompt, 
                    request.ConversationId, 
                    request.Provider,
                    patientId);
                
                Console.WriteLine($"ChatController returning response: '{response.Message}'");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChatController error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("regular")]
        public async Task<ActionResult<ChatResponse>> SendRegularMessage([FromBody] ChatRequest request)
        {
            try
            {
                var response = await _chatService.SendRegularMessageAsync(
                    request.Prompt, 
                    request.ConversationId, 
                    request.Provider);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public AiProvider Provider { get; set; } = AiProvider.HuggingFace;
        public int PatientId { get; set; } = 0;
    }
}
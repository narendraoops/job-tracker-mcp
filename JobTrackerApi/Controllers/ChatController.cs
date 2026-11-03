using JobTrackerApi.Models;
using JobTrackerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobTrackerApi.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<ActionResult<ChatReply>> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Question is required.");
            }

            try
            {
                ChatReply reply = await _chatService.AskAsync(request.Question);
                return Ok(reply);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "The assistant could not answer: " + ex.Message);
            }
        }
    }
}

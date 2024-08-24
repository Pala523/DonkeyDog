using DonkeyDog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace DonkeyDog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly FeedbackService _feedbackService;

        public FeedbackController(FeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        // POST: api/feedback
        [HttpPost]
        public async Task<IActionResult> Create(Feedback feedback)
        {
            if (feedback == null)
            {
                return BadRequest("Feedback cannot be null");
            }

            await _feedbackService.CreateAsync(feedback);
            return CreatedAtAction(nameof(GetAll), new { id = feedback.Id }, feedback);
        }

        // GET: api/feedback
        [HttpGet]
        public async Task<ActionResult<List<Feedback>>> GetAll()
        {
            var feedbacks = await _feedbackService.GetAllAsync();
            return Ok(feedbacks);
        }

        // DELETE: api/feedback/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _feedbackService.DeleteAsync(id);
            return NoContent(); 
        }
    }

}

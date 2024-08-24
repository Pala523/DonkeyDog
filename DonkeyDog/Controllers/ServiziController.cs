using DonkeyDog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace DonkeyDog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiziController : ControllerBase
    {
        private readonly ServiziService _serviziService;

        public ServiziController(ServiziService serviziService)
        {
            _serviziService = serviziService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Create(Servizi servizio)
        {
            await _serviziService.CreateAsync(servizio);
            return CreatedAtAction(nameof(GetById), new { id = servizio.Id }, servizio);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> Update(Guid id, Servizi servizio)
        {
            var existingServizio = await _serviziService.GetByIdAsync(id);
            if (existingServizio == null) return NotFound();

            servizio.Id = id; // Mantieni l'ID
            await _serviziService.UpdateAsync(id, servizio);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(Guid id)
        {
            var existingServizio = await _serviziService.GetByIdAsync(id);
            if (existingServizio == null) return NotFound();

            await _serviziService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<List<Servizi>>> GetAll()
        {
            var servizi = await _serviziService.GetAllAsync();
            return Ok(servizi);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Servizi>> GetById(Guid id)
        {
            var servizio = await _serviziService.GetByIdAsync(id);
            if (servizio == null) return NotFound();
            return Ok(servizio);
        }
    }
}

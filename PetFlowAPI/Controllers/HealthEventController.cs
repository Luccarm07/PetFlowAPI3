using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFlowAPI.Data;
using PetFlowAPI.DTOs;
using PetFlowAPI.Models;
using PetFlowAPI.Enums;

namespace PetFlowAPI.Controllers
{
    [ApiController]
    [Route("health-events")]
    public class HealthEventController : ControllerBase
    {
        private readonly PetFlowContext _context;
        private readonly IMapper _mapper;

        public HealthEventController(PetFlowContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>Registra um novo evento de saúde para um pet.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(HealthEventResponseDTO), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<HealthEventResponseDTO>> Create(HealthEventRequestDTO request)
        {
            var healthEvent = _mapper.Map<HealthEvent>(request);
            _context.HealthEvents.Add(healthEvent);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<HealthEventResponseDTO>(healthEvent);
            return CreatedAtAction(nameof(GetById), new { id = healthEvent.Id }, response);
        }

        /// <summary>Lista eventos de saúde com paginação e filtros por petId, status e ordenação.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<HealthEventResponseDTO>), 200)]
        public async Task<ActionResult<IEnumerable<HealthEventResponseDTO>>> GetAll(
            [FromQuery] long? petId,
            [FromQuery] string? status,
            [FromQuery] int page = 0,
            [FromQuery] int size = 10,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? direction = "desc")
        {
            var query = _context.HealthEvents.AsQueryable();

            if (petId.HasValue)
                query = query.Where(e => e.PetId == petId);

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<HealthEventStatus>(status, true, out var statusEnum))
                    query = query.Where(e => e.Status == statusEnum);
            }

            var events = await query
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<HealthEventResponseDTO>>(events));
        }

        /// <summary>Busca um evento de saúde pelo ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(HealthEventResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<HealthEventResponseDTO>> GetById(long id)
        {
            var healthEvent = await _context.HealthEvents.FindAsync(id);

            if (healthEvent == null)
                return NotFound(new { message = $"Evento de saúde com ID {id} não encontrado." });

            return Ok(_mapper.Map<HealthEventResponseDTO>(healthEvent));
        }

        /// <summary>Atualiza os dados de um evento de saúde.</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(HealthEventResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(long id, HealthEventRequestDTO request)
        {
            var healthEvent = await _context.HealthEvents.FindAsync(id);

            if (healthEvent == null)
                return NotFound(new { message = $"Evento de saúde com ID {id} não encontrado." });

            _mapper.Map(request, healthEvent);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HealthEventExists(id))
                    return NotFound(new { message = $"Evento de saúde com ID {id} não encontrado." });
                else
                    throw;
            }

            return Ok(_mapper.Map<HealthEventResponseDTO>(healthEvent));
        }

        /// <summary>Remove um evento de saúde pelo ID.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(long id)
        {
            var healthEvent = await _context.HealthEvents.FindAsync(id);
            if (healthEvent == null)
                return NotFound(new { message = $"Evento de saúde com ID {id} não encontrado." });

            _context.HealthEvents.Remove(healthEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool HealthEventExists(long id)
        {
            return _context.HealthEvents.Any(e => e.Id == id);
        }
    }
}

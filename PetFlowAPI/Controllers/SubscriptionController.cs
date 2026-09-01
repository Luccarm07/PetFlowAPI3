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
    [Route("subscriptions")]
    public class SubscriptionController : ControllerBase
    {
        private readonly PetFlowContext _context;
        private readonly IMapper _mapper;

        public SubscriptionController(PetFlowContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>Cria uma nova assinatura de plano para um pet.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(SubscriptionResponseDTO), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<SubscriptionResponseDTO>> Create(SubscriptionRequestDTO request)
        {
            var subscription = _mapper.Map<Subscription>(request);
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<SubscriptionResponseDTO>(subscription);
            return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, response);
        }

        /// <summary>Lista assinaturas com paginação e filtros por petId, status e ordenação.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionResponseDTO>), 200)]
        public async Task<ActionResult<IEnumerable<SubscriptionResponseDTO>>> GetAll(
            [FromQuery] long? petId,
            [FromQuery] string? status,
            [FromQuery] int page = 0,
            [FromQuery] int size = 10,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? direction = "desc")
        {
            var query = _context.Subscriptions.AsQueryable();

            if (petId.HasValue)
                query = query.Where(s => s.PetId == petId);

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<SubscriptionStatus>(status, true, out var statusEnum))
                    query = query.Where(s => s.Status == statusEnum);
            }

            var subscriptions = await query
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<SubscriptionResponseDTO>>(subscriptions));
        }

        /// <summary>Busca uma assinatura pelo ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SubscriptionResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SubscriptionResponseDTO>> GetById(long id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);

            if (subscription == null)
                return NotFound(new { message = $"Assinatura com ID {id} não encontrada." });

            return Ok(_mapper.Map<SubscriptionResponseDTO>(subscription));
        }

        /// <summary>Atualiza os dados de uma assinatura.</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SubscriptionResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(long id, SubscriptionRequestDTO request)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);

            if (subscription == null)
                return NotFound(new { message = $"Assinatura com ID {id} não encontrada." });

            _mapper.Map(request, subscription);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubscriptionExists(id))
                    return NotFound(new { message = $"Assinatura com ID {id} não encontrada." });
                else
                    throw;
            }

            return Ok(_mapper.Map<SubscriptionResponseDTO>(subscription));
        }

        /// <summary>Remove uma assinatura pelo ID.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(long id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
                return NotFound(new { message = $"Assinatura com ID {id} não encontrada." });

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>Atualiza o status de uma assinatura .</summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(SubscriptionResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(long id, [FromQuery] string status)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);

            if (subscription == null)
                return NotFound(new { message = $"Assinatura com ID {id} não encontrada." });

            if (Enum.TryParse<SubscriptionStatus>(status, true, out var newStatus))
            {
                subscription.Status = newStatus;
                await _context.SaveChangesAsync();
                return Ok(_mapper.Map<SubscriptionResponseDTO>(subscription));
            }

            return BadRequest(new { message = "Status inválido. Valores aceitos: Active, Cancelled, Suspended." });
        }

        private bool SubscriptionExists(long id)
        {
            return _context.Subscriptions.Any(e => e.Id == id);
        }
    }
}

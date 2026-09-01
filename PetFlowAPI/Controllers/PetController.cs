using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFlowAPI.Data;
using PetFlowAPI.DTOs;
using PetFlowAPI.Models;

namespace PetFlowAPI.Controllers
{
    [ApiController]
    [Route("pets")]
    public class PetController : ControllerBase
    {
        private readonly PetFlowContext _context;
        private readonly IMapper _mapper;

        public PetController(PetFlowContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>Cadastra um novo pet.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(PetResponseDTO), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PetResponseDTO>> Create(PetRequestDTO request)
        {
            var pet = _mapper.Map<Pet>(request);
            _context.Pets.Add(pet);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<PetResponseDTO>(pet);
            return CreatedAtAction(nameof(GetById), new { id = pet.Id }, response);
        }

        /// <summary>Lista todos os pets com paginação e filtro por nome.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PetResponseDTO>), 200)]
        public async Task<ActionResult<IEnumerable<PetResponseDTO>>> GetAll(
            [FromQuery] string? name,
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            var query = _context.Pets.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(p => p.Name.Contains(name));

            var pets = await query
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<PetResponseDTO>>(pets));
        }

        /// <summary>Busca um pet pelo ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PetResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PetResponseDTO>> GetById(long id)
        {
            var pet = await _context.Pets.FindAsync(id);

            if (pet == null)
                return NotFound(new { message = $"Pet com ID {id} não encontrado." });

            return Ok(_mapper.Map<PetResponseDTO>(pet));
        }

        /// <summary>Atualiza os dados de um pet.</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PetResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(long id, PetRequestDTO request)
        {
            var pet = await _context.Pets.FindAsync(id);

            if (pet == null)
                return NotFound(new { message = $"Pet com ID {id} não encontrado." });

            _mapper.Map(request, pet);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PetExists(id))
                    return NotFound(new { message = $"Pet com ID {id} não encontrado." });
                else
                    throw;
            }

            return Ok(_mapper.Map<PetResponseDTO>(pet));
        }

        /// <summary>Remove um pet e todos os seus registros dependentes em cascata.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(long id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet == null)
                return NotFound(new { message = $"Pet com ID {id} não encontrado." });

            // Remove filhos antes de remover o pet (FK Oracle)
            var healthEvents = await _context.HealthEvents
                .Where(h => h.PetId == id).ToListAsync();
            _context.HealthEvents.RemoveRange(healthEvents);

            var subscriptions = await _context.Subscriptions
                .Where(s => s.PetId == id).ToListAsync();
            _context.Subscriptions.RemoveRange(subscriptions);

            var riskScores = await _context.RiskScores
                .Where(rs => rs.PetId == id).ToListAsync();
            _context.RiskScores.RemoveRange(riskScores);

            _context.Pets.Remove(pet);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PetExists(long id)
        {
            return _context.Pets.Any(e => e.Id == id);
        }
    }
}

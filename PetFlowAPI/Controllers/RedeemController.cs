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
    [Route("redeems")]
    public class RedeemController : ControllerBase
    {
        private readonly PetFlowContext _context;
        private readonly IMapper _mapper;

        public RedeemController(PetFlowContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>Registra o resgate de um cupom por um tutor.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(RedeemResponseDTO), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<RedeemResponseDTO>> Create(RedeemRequestDTO request)
        {
            var redeem = _mapper.Map<Redeem>(request);
            _context.Redeems.Add(redeem);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<RedeemResponseDTO>(redeem);
            return CreatedAtAction(nameof(GetById), new { id = redeem.Id }, response);
        }

        /// <summary>Lista resgates com paginação e filtro por tutorId e ordenação.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RedeemResponseDTO>), 200)]
        public async Task<ActionResult<IEnumerable<RedeemResponseDTO>>> GetAll(
            [FromQuery] long? tutorId,
            [FromQuery] int page = 0,
            [FromQuery] int size = 10,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? direction = "desc")
        {
            var query = _context.Redeems.AsQueryable();

            if (tutorId.HasValue)
                query = query.Where(r => r.TutorId == tutorId);

            var redeems = await query
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<RedeemResponseDTO>>(redeems));
        }

        /// <summary>Busca um resgate pelo ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RedeemResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RedeemResponseDTO>> GetById(long id)
        {
            var redeem = await _context.Redeems.FindAsync(id);

            if (redeem == null)
                return NotFound(new { message = $"Resgate com ID {id} não encontrado." });

            return Ok(_mapper.Map<RedeemResponseDTO>(redeem));
        }

        /// <summary>Remove um resgate pelo ID. Recurso imutável por design; use apenas para correções.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(long id)
        {
            var redeem = await _context.Redeems.FindAsync(id);
            if (redeem == null)
                return NotFound(new { message = $"Resgate com ID {id} não encontrado." });

            _context.Redeems.Remove(redeem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

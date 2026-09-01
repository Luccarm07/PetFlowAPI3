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
    [Route("coupons")]
    public class CouponController : ControllerBase
    {
        private readonly PetFlowContext _context;
        private readonly IMapper _mapper;

        public CouponController(PetFlowContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>Cria um novo cupom de desconto.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(CouponResponseDTO), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CouponResponseDTO>> Create(CouponRequestDTO request)
        {
            if (request.ExpirationDate.HasValue && request.ExpirationDate.Value.Date < DateTime.Today)
                return BadRequest(new
                {
                    campo = "expirationDate",
                    mensagem = "A data de expiração não pode ser uma data no passado."
                });

            var coupon = _mapper.Map<Coupon>(request);
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<CouponResponseDTO>(coupon);
            return CreatedAtAction(nameof(GetById), new { id = coupon.Id }, response);
        }

        /// <summary>Lista todos os cupons com paginação e filtro por código.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CouponResponseDTO>), 200)]
        public async Task<ActionResult<IEnumerable<CouponResponseDTO>>> GetAll(
            [FromQuery] string? code,
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            var query = _context.Coupons.AsQueryable();

            if (!string.IsNullOrEmpty(code))
                query = query.Where(c => c.Code.Contains(code));

            var coupons = await query
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<CouponResponseDTO>>(coupons));
        }

        /// <summary>Busca um cupom pelo ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CouponResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CouponResponseDTO>> GetById(long id)
        {
            var coupon = await _context.Coupons.FindAsync(id);

            if (coupon == null)
                return NotFound(new { message = $"Cupom com ID {id} não encontrado." });

            return Ok(_mapper.Map<CouponResponseDTO>(coupon));
        }

        /// <summary>Atualiza os dados de um cupom.</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CouponResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(long id, CouponRequestDTO request)
        {
            if (request.ExpirationDate.HasValue && request.ExpirationDate.Value.Date < DateTime.Today)
                return BadRequest(new
                {
                    campo = "expirationDate",
                    mensagem = "A data de expiração não pode ser uma data no passado."
                });

            var coupon = await _context.Coupons.FindAsync(id);

            if (coupon == null)
                return NotFound(new { message = $"Cupom com ID {id} não encontrado." });

            _mapper.Map(request, coupon);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CouponExists(id))
                    return NotFound(new { message = $"Cupom com ID {id} não encontrado." });
                else
                    throw;
            }

            return Ok(_mapper.Map<CouponResponseDTO>(coupon));
        }

        /// <summary>Remove um cupom e seus resgates associados.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(long id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
                return NotFound(new { message = $"Cupom com ID {id} não encontrado." });

            var redeems = await _context.Redeems
                .Where(r => r.CouponId == id).ToListAsync();
            _context.Redeems.RemoveRange(redeems);

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>Atualiza o status de um cupom.</summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(CouponResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(long id, [FromQuery] string status)
        {
            var coupon = await _context.Coupons.FindAsync(id);

            if (coupon == null)
                return NotFound(new { message = $"Cupom com ID {id} não encontrado." });

            if (Enum.TryParse<CouponStatus>(status, true, out var newStatus))
            {
                coupon.Status = newStatus;
                await _context.SaveChangesAsync();
                return Ok(_mapper.Map<CouponResponseDTO>(coupon));
            }

            return BadRequest(new { message = "Status inválido. Valores aceitos: Active, Used, Expired." });
        }

        private bool CouponExists(long id)
        {
            return _context.Coupons.Any(e => e.Id == id);
        }
    }
}

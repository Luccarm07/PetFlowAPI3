using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFlowAPI.Data;
using PetFlowAPI.DTOs;
using PetFlowAPI.Models;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace PetFlowAPI.Controllers
{
    [ApiController]
    [Route("clinics")]
    public class ClinicController : ControllerBase
    {
        private readonly PetFlowContext _context;
        private readonly IMapper _mapper;
        private readonly IHostEnvironment _environment;

        public ClinicController(PetFlowContext context, IMapper mapper, IHostEnvironment environment)
        {
            _context = context;
            _mapper = mapper;
            _environment = environment;
        }

        /// <summary>Cria uma nova clínica.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ClinicResponseDTO), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ClinicResponseDTO>> Create(ClinicRequestDTO request)
        {
            var clinic = _mapper.Map<Clinic>(request);
            _context.Clinics.Add(clinic);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ClinicResponseDTO>(clinic);
            return CreatedAtAction(nameof(GetById), new { id = clinic.Id }, response);
        }

        /// <summary>Lista todas as clínicas com paginação e filtro por nome.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClinicResponseDTO>), 200)]
        public async Task<ActionResult<IEnumerable<ClinicResponseDTO>>> GetAll(
            [FromQuery] string? name,
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            var query = _context.Clinics.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(c => c.Name.Contains(name));

            var clinics = await query
                .OrderBy(c => c.Id)
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<ClinicResponseDTO>>(clinics));
        }

        /// <summary>Busca uma clínica pelo ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClinicResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ClinicResponseDTO>> GetById(long id)
        {
            var clinic = await _context.Clinics.FindAsync(id);

            if (clinic == null)
                return NotFound(new { message = $"Clínica com ID {id} não encontrada." });

            return Ok(_mapper.Map<ClinicResponseDTO>(clinic));
        }

        /// <summary>Atualiza os dados de uma clínica.</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ClinicResponseDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(long id, ClinicRequestDTO request)
        {
            var clinic = await _context.Clinics.FindAsync(id);

            if (clinic == null)
                return NotFound(new { message = $"Clínica com ID {id} não encontrada." });

            _mapper.Map(request, clinic);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClinicExists(id))
                    return NotFound(new { message = $"Clínica com ID {id} não encontrada." });
                throw;
            }

            return Ok(_mapper.Map<ClinicResponseDTO>(clinic));
        }

        /// <summary>
        /// Remove uma clínica e todos os seus registros dependentes em cascata.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(long id)
        {
            // Evita AnyAsync, pois algumas versões do provider Oracle traduzem
            // o resultado booleano para FALSE/TRUE, que não são literais SQL válidos.
            var clinic = await _context.Clinics
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clinic == null)
                return NotFound(new { message = $"Clínica com ID {id} não encontrada." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // O DDL Oracle não usa ON DELETE CASCADE. Remover sempre do filho para o pai.
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM REDEEM
                    WHERE COUPON_ID IN (
                        SELECT C.ID FROM COUPON C
                        JOIN COUPON_TEMPLATE CT ON CT.ID = C.TEMPLATE_ID
                        JOIN PARTNER_DISCOUNT PD ON PD.ID = CT.PARTNER_DISCOUNT_ID
                        WHERE PD.CLINIC_ID = {id}
                    )
                    """);

                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM COUPON
                    WHERE TEMPLATE_ID IN (
                        SELECT CT.ID FROM COUPON_TEMPLATE CT
                        JOIN PARTNER_DISCOUNT PD ON PD.ID = CT.PARTNER_DISCOUNT_ID
                        WHERE PD.CLINIC_ID = {id}
                    )
                    """);

                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM COUPON_TEMPLATE
                    WHERE PARTNER_DISCOUNT_ID IN (
                        SELECT ID FROM PARTNER_DISCOUNT WHERE CLINIC_ID = {id}
                    )
                    """);

                await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM PARTNER_DISCOUNT WHERE CLINIC_ID = {id}");

                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM SUBSCRIPTION
                    WHERE PLAN_ID IN (SELECT ID FROM PLAN WHERE CLINIC_ID = {id})
                    """);

                await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM PLAN WHERE CLINIC_ID = {id}");
                await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM HEALTH_EVENT WHERE CLINIC_ID = {id}");
                await _context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM CLINIC WHERE ID = {id}");

                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Log.Error(ex, "Falha ao excluir a clínica {ClinicId}", id);

                var message = ex.ToString();
                var isForeignKey = message.Contains("ORA-02291", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("ORA-02292", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("integrity constraint", StringComparison.OrdinalIgnoreCase);

                if (isForeignKey)
                {
                    return Conflict(new
                    {
                        statusCode = 409,
                        erro = "A clínica ainda possui registros relacionados.",
                        detalhe = _environment.IsDevelopment() ? message : null
                    });
                }

                throw;
            }
        }
        private bool ClinicExists(long id)
        {
            return _context.Clinics.Any(e => e.Id == id);
        }
    }
}

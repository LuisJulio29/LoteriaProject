using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoteriaProject.Context;
using LoteriaProject.Model;

namespace LoteriaProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SorteosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SorteosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Sorteos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sorteo>>> GetSorteos()
        {
            return await _context.Sorteos.ToListAsync();
        }

        // GET: api/Sorteos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Sorteo>> GetSorteo(int id)
        {
            var sorteo = await _context.Sorteos.FindAsync(id);

            if (sorteo == null)
            {
                return NotFound();
            }

            return sorteo;
        }

        [HttpGet("GetSorteoByNumber")]
        public async Task<ActionResult<IEnumerable<Sorteo>>> GetSorteoByNumber([FromQuery] string? number = null, [FromQuery] string? serie = null)
        {
            // Si ambos parámetros están vacíos, retornar NotFound
            if (string.IsNullOrEmpty(number) && string.IsNullOrEmpty(serie))
            {
                return NotFound("Debe proporcionar al menos un criterio de búsqueda");
            }

            // Inicializar las variables para las permutaciones
            HashSet<string>? numbers = null;
            HashSet<string>? series = null;
            string? lastThreeDigits = null;

            // Generar permutaciones solo si los parámetros no están vacíos
            if (!string.IsNullOrEmpty(number))
            {
                numbers = GeneratePermutations(number);
                lastThreeDigits = number.Length >= 3 ? number.Substring(number.Length - 3) : number;
            }

            if (!string.IsNullOrEmpty(serie))
            {
                series = GeneratePermutations(serie);
            }

            // Construir la consulta base
            var query = _context.Sorteos.AsQueryable();

            // Aplicar filtros según los parámetros proporcionados
            if (numbers != null && series != null)
            {
                // Búsqueda con ambos criterios
                query = query.Where(s =>
                    (numbers.Contains(s.Number) && series.Contains(s.Serie)) ||
                    (s.Number.EndsWith(lastThreeDigits) && series.Contains(s.Serie))
                );
            }
            else if (numbers != null)
            {
                // Búsqueda solo por número
                query = query.Where(s =>
                    numbers.Contains(s.Number) ||
                    s.Number.EndsWith(lastThreeDigits)
                );
            }
            else if (series != null)
            {
                // Búsqueda solo por serie
                query = query.Where(s => series.Contains(s.Serie));
            }

            var sorteos = await query.ToListAsync();

            if (!sorteos.Any())
            {
                return NotFound("No se encontraron sorteos");
            }

            return sorteos;
        }

        [HttpGet("GetSorteoByDate")]
        public async Task<ActionResult<IEnumerable<Sorteo>>> GetSorteoByDate([FromQuery] DateTime date)
        {
            var sorteos = await _context.Sorteos
                .Where(s => s.Date.Date == date.Date ).ToListAsync();
            if (!sorteos.Any())
            {
                return NotFound("No se encontraron sorteos");
            }
            return sorteos;
        }
        // PUT: api/Sorteos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSorteo(int id, Sorteo sorteo)
        {
            if (id != sorteo.Id)
            {
                return BadRequest();
            }

            _context.Entry(sorteo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SorteoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Sorteos
        [HttpPost]
        public async Task<ActionResult<Sorteo>> PostSorteo(Sorteo sorteo)
        {
            try
            {
                await ValidateSorteo(sorteo);
                _context.Sorteos.Add(sorteo);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetSorteo", new { id = sorteo.Id }, sorteo);
            }
            catch (SorteoValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Sorteos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSorteo(int id)
        {
            var sorteo = await _context.Sorteos.FindAsync(id);
            if (sorteo == null)
            {
                return NotFound();
            }

            _context.Sorteos.Remove(sorteo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class SorteoValidationException : Exception
        {
            public SorteoValidationException(string message) : base(message) { }
        }
        private HashSet<string> GeneratePermutations(string number)
        {
            var result = new HashSet<string>();
            if (number.Length <= 1)
            {
                result.Add(number);
                return result;
            }

            for (int i = 0; i < number.Length; i++)
            {
                char currentChar = number[i];
                string remainingChars = number.Substring(0, i) + number.Substring(i + 1);
                var subPermutations = GeneratePermutations(remainingChars);
                foreach (string subPermutation in subPermutations)
                {
                    result.Add(currentChar + subPermutation);
                }
            }
            return result;
        }

        private async Task ValidateSorteo(Sorteo sorteo)
        {
            // Validación básica
            if (string.IsNullOrEmpty(sorteo.Number) || string.IsNullOrEmpty(sorteo.Serie) || sorteo.Date == default)
            {
                throw new SorteoValidationException("Datos del ticket incompletos");
            }

            // Validación de duplicados
            var isDuplicate = await _context.Sorteos
                .AnyAsync(e => e.Number == sorteo.Number
                              && e.Date.Date == sorteo.Date.Date &&
                              e.Loteria == sorteo.Loteria);

            if (isDuplicate)
            {
                throw new SorteoValidationException($"Ya existe una Sorteo con el número {sorteo.Number} - {sorteo.Serie} para la loteria {sorteo.Loteria} en la fecha{sorteo.Date.Date:dd/MM/yyyy}");
            }
        }
        private bool SorteoExists(int id)
        {
            return _context.Sorteos.Any(e => e.Id == id);
        }
    }
}

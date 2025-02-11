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
    public class PatronsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PatronsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Patrons
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patron>>> GetPatrons()
        {
            return await _context.Patrons.ToListAsync();
        }

        // GET: api/Patrons/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Patron>> GetPatron(int id)
        {
            var patron = await _context.Patrons.FindAsync(id);

            if (patron == null)
            {
                return NotFound();
            }

            return patron;
        }

        [HttpGet("Search")]
        public async Task<ActionResult<Patron>> GetPatronByDate([FromQuery] DateTime date, [FromQuery] string Jornada)
        {
            var patron = await _context.Patrons
                .Where(p => p.Date.Date == date.Date && p.Jornada == Jornada)
                .FirstOrDefaultAsync();

            if (patron == null)
            {
                return NotFound();
            }
            return patron;
        }

        [HttpPost("CalculateRedundancy")]
        public async Task<ActionResult<List<PatronRedundancy>>> CalculateRedundancy([FromBody] Patron patron)
        {
            var allPatrons = await _context.Patrons.Where(p => p.Id != patron.Id).ToListAsync();
            var redundancies = new List<PatronRedundancy>();
            foreach (var existingPatron in allPatrons)
            {
                int matchCount = 0;
                for (int i = 0; i < 10; i++)
                {
                    if (patron.PatronNumbers[i] == existingPatron.PatronNumbers[i])
                    {
                        matchCount++;
                    }
                }
                if (matchCount > 3)
                {
                    redundancies.Add(new PatronRedundancy
                    {
                        Patron = existingPatron,
                        RedundancyCount = matchCount
                    });
                }
            }
            return Ok(redundancies.OrderByDescending(r => r.RedundancyCount).ToList());
        }

        [HttpGet("GetRedundancyinDate")]
        public async Task<ActionResult<List<Patron>>> RedundancyinDate([FromQuery] DateTime date)
        {
            var allPatrons = await _context.Patrons.Where(p => p.Date.Day == date.Day && p.Date.DayOfWeek == date.DayOfWeek && p.Date.Date != date.Date).ToListAsync();
            if (allPatrons == null)
            {
                return NotFound();
            }
            return Ok(allPatrons);

        }

        [HttpGet("GetNumbersNotPlayed")]
        public async Task<ActionResult<string[]>> NumberNotPlayed([FromQuery]DateTime date, [FromQuery] string Jornada)
        {
            var tickets = await _context.Tickets.Where(t => t.Date.Date == date.Date && t.Jornada == Jornada).ToListAsync();
            if (tickets == null || !tickets.Any())
            {
                return NotFound("No se encontraron Tickets");
            }

            // Inicializar arrays para marcar qué números aparecen en cada posición
            bool[] usedRow1 = new bool[10];
            bool[] usedRow2 = new bool[10];
            bool[] usedRow3 = new bool[10];
            bool[] usedRow4 = new bool[10];

            // Marcar números usados en cada posición
            foreach (var ticket in tickets)
            {
                if (ticket.Number.Length == 4)
                {
                    usedRow1[int.Parse(ticket.Number[0].ToString())] = true;
                    usedRow2[int.Parse(ticket.Number[1].ToString())] = true;
                    usedRow3[int.Parse(ticket.Number[2].ToString())] = true;
                    usedRow4[int.Parse(ticket.Number[3].ToString())] = true;
                }
            }

            // Obtener números no usados en cada posición
            List<int> notUsedRow1 = GetNotUsedNumbers(usedRow1);
            List<int> notUsedRow2 = GetNotUsedNumbers(usedRow2);
            List<int> notUsedRow3 = GetNotUsedNumbers(usedRow3);
            List<int> notUsedRow4 = GetNotUsedNumbers(usedRow4);

            // Formar los números de 4 dígitos
            List<string> result = new List<string>();

            // Si alguna posición no tiene números no usados, usar "*"
            if (!notUsedRow1.Any() || !notUsedRow2.Any() || !notUsedRow3.Any() || !notUsedRow4.Any())
            {
                string number = "";
                number += notUsedRow1.Any() ? notUsedRow1[0].ToString() : "*";
                number += notUsedRow2.Any() ? notUsedRow2[0].ToString() : "*";
                number += notUsedRow3.Any() ? notUsedRow3[0].ToString() : "*";
                number += notUsedRow4.Any() ? notUsedRow4[0].ToString() : "*";
                result.Add(number);
            }
            else
            {
                // Tomar un número de cada fila en orden para formar los números
                for (int i = 0; i < Math.Max(Math.Max(notUsedRow1.Count, notUsedRow2.Count),
                                           Math.Max(notUsedRow3.Count, notUsedRow4.Count)); i++)
                {
                    string number = "";
                    number += i < notUsedRow1.Count ? notUsedRow1[i].ToString() : "*";
                    number += i < notUsedRow2.Count ? notUsedRow2[i].ToString() : "*";
                    number += i < notUsedRow3.Count ? notUsedRow3[i].ToString() : "*";
                    number += i < notUsedRow4.Count ? notUsedRow4[i].ToString() : "*";
                    result.Add(number);
                }
            }
            var NumbersNotplayed = result.ToArray();
            return Ok(NumbersNotplayed);
        }

        [HttpGet("GetVoidinDay/{id}")]
        public async Task<ActionResult<List<Patron>>> VoidinDay(int id)
        {
            // Buscar el patrón de referencia de forma asíncrona
            var patron = await _context.Patrons.FindAsync(id);
            if (patron == null)
            {
                return NotFound($"No se encontró el patrón con ID {id}");
            }
            // Validar que PatronNumbers no sea null
            if (patron.PatronNumbers == null || patron.PatronNumbers.Length == 0)
            {
                return BadRequest("El patrón de referencia no tiene números válidos");
            }
            if (!patron.PatronNumbers.Contains(0))
            {
                return NotFound("No contiene ningun 0 en su Patron");
            }
            try
            {
                // Obtener todos los patrones que coincidan con el día y jornada, excluyendo el patrón de referencia
                var patrons = await _context.Patrons
                    .Where(p => p.Id != patron.Id && p.Date.Day == patron.Date.Day && p.Jornada == patron.Jornada).ToListAsync();
                if (!patrons.Any())
                {
                    return NotFound("No hay otros patrones que cumplan con los requisitos de fecha y jornada");
                }
                // Obtener índices de ceros del patrón de referencia
                var zeroIndices = new HashSet<int>(); // Usando HashSet para búsquedas más eficientes
                for (int i = 0; i < patron.PatronNumbers.Length; i++)
                {
                    if (patron.PatronNumbers[i] == 0)
                    {
                        zeroIndices.Add(i);
                    }
                }
                // Filtrar los patrones que coincidan en los mismos ceros
                var matchingPatrons = patrons
                    .Where(p => PatronMatchesZeroPattern(p, patron.PatronNumbers.Length, zeroIndices))
                    .ToList();
                if (!matchingPatrons.Any())
                {
                    return NotFound("No se encontraron otros patrones con los mismos ceros");
                }

                return Ok(matchingPatrons);
            }
            catch (Exception ex)
            {
                // Log the exception here if you have logging configured
                return StatusCode(500, "Error interno al procesar la solicitud");
            }
        }
        // PUT: api/Patrons/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPatron(int id, Patron patron)
        {
            if (id != patron.Id)
            {
                return BadRequest();
            }

            _context.Entry(patron).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatronExists(id))
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

        // POST: api/Patrons
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Patron>> PostPatron(Patron patron)
        {
            try
            {
                await ValidatePatron(patron);
                _context.Patrons.Add(patron);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetPatron", new { id = patron.Id }, patron);
            }
            catch (PatronValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("Calculate")]
        public async Task<ActionResult<Patron>> CalculatePatron([FromQuery] DateTime date, [FromQuery] string Jornada)
        {
            var jornadasToSearch = Jornada.ToLower() == "dia" ? new[] { "Dia", "Tarde" } : new[] { Jornada };

            var tickets = await _context.Tickets
                .Where(t => t.Date.Date == date.Date && jornadasToSearch.Contains(t.Jornada))
                .ToListAsync();

            if (!tickets.Any())
            {
                return NotFound($"No se encontraron tickets para la fecha {date.ToShortDateString()} y jornada {Jornada}");
            }

            int[] patronNumbers = new int[10];
            foreach (var ticket in tickets)
            {
                foreach (char digit in ticket.Number)
                {
                    if (int.TryParse(digit.ToString(), out int number))
                    {
                        patronNumbers[number]++;
                    }
                }
            }

            var patron = new Patron
            {
                Date = date.Date,
                Jornada = Jornada,
                PatronNumbers = patronNumbers
            };

            try
            {
                await ValidatePatron(patron);
                _context.Patrons.Add(patron);
                await _context.SaveChangesAsync();
                return Ok(patron);
            }
            catch (PatronValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // DELETE: api/Patrons/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatron(int id)
        {
            var patron = await _context.Patrons.FindAsync(id);
            if (patron == null)
            {
                return NotFound();
            }

            _context.Patrons.Remove(patron);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PatronExists(int id)
        {
            return _context.Patrons.Any(e => e.Id == id);
        }
        public class PatronRedundancy
        {
            public required Patron Patron { get; set; }
            public int RedundancyCount { get; set; }
        }

        private List<int> GetNotUsedNumbers(bool[] usedNumbers)
        {
            var notUsed = new List<int>();
            for (int i = 0; i < usedNumbers.Length; i++)
            {
                if (!usedNumbers[i])
                {
                    notUsed.Add(i);
                }
            }
            return notUsed.OrderBy(x => x).ToList();
        }
        private static bool PatronMatchesZeroPattern(Patron patron, int referenceLength, HashSet<int> zeroIndices)
        {
            // Verificar longitud
            if (patron.PatronNumbers == null || patron.PatronNumbers.Length != referenceLength)
                return false;

            // Verificar que tenga ceros en las mismas posiciones
            foreach (var index in zeroIndices)
            {
                if (patron.PatronNumbers[index] != 0)
                {
                    return false;
                }
            }

            // Verificar que no tenga ceros adicionales
            for (int i = 0; i < patron.PatronNumbers.Length; i++)
            {
                if (!zeroIndices.Contains(i) && patron.PatronNumbers[i] == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public class PatronValidationException : Exception
        {
            public PatronValidationException(string message) : base(message) { }
        }

        private async Task ValidatePatron(Patron patron)
        {
            if (patron.PatronNumbers == null || patron.PatronNumbers.Length != 10)
            {
                throw new PatronValidationException("El patrón debe contener exactamente 10 números");
            }

            var existingPatron = await _context.Patrons
                .FirstOrDefaultAsync(p => p.Date.Date == patron.Date.Date &&
                                         p.Jornada == patron.Jornada);

            if (existingPatron != null)
            {
                if (patron.PatronNumbers.SequenceEqual(existingPatron.PatronNumbers))
                {
                    throw new PatronValidationException($"Ya existe un patrón idéntico para la fecha {patron.Date.Date:dd/MM/yyyy} y jornada {patron.Jornada}");
                }
            }
        }
     }
}

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
                if (matchCount > 0)
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
            _context.Patrons.Add(patron);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPatron", new { id = patron.Id }, patron);
        }

        [HttpPost("Calculate")]
        public async Task<ActionResult<Patron>> CalculatePatron([FromQuery] DateTime date, [FromQuery] string Jornada)
        {
            var jornadasToSearch = Jornada.ToLower() == "dia"? new[] { "Dia", "Tarde" }: new[] { Jornada };

            // Obtener tickets que coincidan con la fecha y jornada
            var tickets = await _context.Tickets
                .Where(t => t.Date.Date == date.Date && jornadasToSearch.Contains(t.Jornada))
                .ToListAsync();
            if (!tickets.Any())
            {
                return NotFound($"No se encontraron tickets para la fecha {date.ToShortDateString()} y jornada {Jornada}");
            }

            // Inicializar array para contar repeticiones (índice 0-9)
            int[] patronNumbers = new int[10];

            // Contar repeticiones de cada número
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

            // Crear nuevo patrón
            var patron = new Patron
            {
                Date = date.Date,
                Jornada = Jornada,
                PatronNumbers = patronNumbers
            };

            // Guardar en la base de datos
            _context.Patrons.Add(patron);
            await _context.SaveChangesAsync();

            return Ok(patron);
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
            public Patron Patron { get; set; }
            public int RedundancyCount { get; set; }
        }

    }
}

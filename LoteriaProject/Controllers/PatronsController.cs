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
            // Obtener tickets que coincidan con la fecha y jornada
            var tickets = await _context.Tickets
                .Where(t => t.Date.Date == date.Date && t.Jornada == Jornada)
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
    }
}

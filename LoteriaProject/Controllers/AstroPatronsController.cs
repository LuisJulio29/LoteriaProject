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
    public class AstroPatronsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AstroPatronsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/AstroPatrons
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AstroPatron>>> GetAstroPatrons()
        {
            return await _context.AstroPatrons.ToListAsync();
        }

        // GET: api/AstroPatrons/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AstroPatron>> GetAstroPatron(int id)
        {
            var astroPatron = await _context.AstroPatrons.FindAsync(id);

            if (astroPatron == null)
            {
                return NotFound();
            }

            return astroPatron;
        }

        [HttpGet("GetAstroPatronByDate")]

        public async Task<ActionResult<AstroPatron>> GetAstroPatronByDate([FromQuery] DateTime date, [FromQuery] string Jornada)
        {
            var astroPatron = await _context.AstroPatrons
                .Where(p => p.Date.Month == date.Month && p.Jornada == Jornada)
                .FirstOrDefaultAsync();
            if (astroPatron == null)
            {
                return NotFound();
            }
            return astroPatron;
        }

        // PUT: api/AstroPatrons/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAstroPatron(int id, AstroPatron astroPatron)
        {
            if (id != astroPatron.Id)
            {
                return BadRequest();
            }

            _context.Entry(astroPatron).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AstroPatronExists(id))
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

        // POST: api/AstroPatrons
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AstroPatron>> PostAstroPatron(AstroPatron astroPatron)
        {
            _context.AstroPatrons.Add(astroPatron);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAstroPatron", new { id = astroPatron.Id }, astroPatron);
        }

        [HttpPost("Calculate")]
        public async Task<IActionResult> Calculate([FromQuery] DateTime date, [FromQuery] string Jornada)
        {
            // Buscar tickets de Astro del mes y jornada específica
            var tickets = await _context.Tickets
                .Where(t => t.Loteria == "Astro" &&
                            t.Date.Month == date.Month &&
                            t.Date.Year == date.Year &&
                            t.Jornada == Jornada)
                .ToListAsync();

            if (!tickets.Any())
            {
                return NotFound($"No se encontraron tickets Astro para el mes {date.Month} y jornada {Jornada}");
            }

            // Buscar si ya existe un patrón para este mes y jornada
            var existingPattern = await _context.AstroPatrons
                .FirstOrDefaultAsync(p => p.Date.Month == date.Month &&
                                         p.Date.Year == date.Year &&
                                         p.Jornada == Jornada);

            // Inicializar arrays para contar repeticiones por fila
            int[] row1 = new int[10]; // 0-9
            int[] row2 = new int[10];
            int[] row3 = new int[10];
            int[] row4 = new int[10];

            // Inicializar array para contar signos
            var signCount = new AstroSign[Enum.GetValues(typeof(AstroSign)).Length];

            // Procesar todos los tickets
            foreach (var ticket in tickets)
            {
                // Procesar cada dígito del número según su posición
                if (ticket.Number.Length == 4)
                {
                    row1[int.Parse(ticket.Number[0].ToString())]++;
                    row2[int.Parse(ticket.Number[1].ToString())]++;
                    row3[int.Parse(ticket.Number[2].ToString())]++;
                    row4[int.Parse(ticket.Number[3].ToString())]++;
                }

                // Contar el signo si existe
                if (!string.IsNullOrEmpty(ticket.sign))
                {
                    if (Enum.TryParse(ticket.sign, out AstroSign signo))
                    {
                        signCount[(int)signo - 1]++;
                    }
                }
            }

            if (existingPattern != null)
            {
                // Actualizar el patrón existente
                existingPattern.Row1 = row1;
                existingPattern.Row2 = row2;
                existingPattern.Row3 = row3;
                existingPattern.Row4 = row4;
                existingPattern.Sign = signCount;
                existingPattern.Date = date; // Actualizar la fecha al último cálculo

                _context.AstroPatrons.Update(existingPattern);
                await _context.SaveChangesAsync();
                return Ok(existingPattern);
            }
            else
            {
                // Crear nuevo patrón si no existe
                var newPattern = new AstroPatron
                {
                    Date = date,
                    Jornada = Jornada,
                    Row1 = row1,
                    Row2 = row2,
                    Row3 = row3,
                    Row4 = row4,
                    Sign = signCount
                };

                _context.AstroPatrons.Add(newPattern);
                await _context.SaveChangesAsync();
                return Ok(newPattern);
            }
        }

        // DELETE: api/AstroPatrons/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAstroPatron(int id)
        {
            var astroPatron = await _context.AstroPatrons.FindAsync(id);
            if (astroPatron == null)
            {
                return NotFound();
            }

            _context.AstroPatrons.Remove(astroPatron);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AstroPatronExists(int id)
        {
            return _context.AstroPatrons.Any(e => e.Id == id);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoteriaProject.Context;
using LoteriaProject.Model;
using LoteriaProject.Custom;

namespace LoteriaProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets()
        {
            return await _context.Tickets.ToListAsync();
        }

        // GET: api/Tickets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Ticket>> GetTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            return ticket;
        }

        [HttpGet("GetTicketByNumber/{number}")]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTicketByNumber(string number)
        {
            var permutations = GeneratePermutations(number);
            string lastThreeDigits = number.Length >= 3 ? number.Substring(number.Length - 3) : number;
            var tickets = await _context.Tickets
                .Where(t => permutations.Contains(t.Number) || (t.Number.Length >= 3 && t.Number.EndsWith(lastThreeDigits))).ToListAsync();
            if (tickets == null || !tickets.Any())
            {
                return NotFound();
            }
            return tickets;
        }

        [HttpGet("GetTicketByDate")]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTicketByDate([FromQuery] DateTime date, [FromQuery] string Jornada)
        {
            var jornadasToSearch = Jornada.ToLower() == "dia" ? new[] { "Dia", "Tarde" } : new[] { Jornada };
            var tickets = await _context.Tickets
                .Where(t => t.Date.Date == date.Date && jornadasToSearch.Contains(t.Jornada)).ToListAsync();
            if (tickets == null || !tickets.Any())
            {
                return NotFound();
            }
            return tickets;
        }
        [HttpGet("GetAstroTicketByDate")]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetAstroTicketByDate([FromQuery] DateTime date, [FromQuery] string Jornada)
        {
            var tickets = await _context.Tickets.Where(t => t.Date.Month == date.Month && t.Date.Year == date.Year && t.Jornada == Jornada && t.Loteria == "Astro").ToListAsync();
            if (tickets == null || !tickets.Any())
            {
                return NotFound();
            }
            return tickets;
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

        // PUT: api/Tickets/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTicket(int id, Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return BadRequest();
            }

            _context.Entry(ticket).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TicketExists(id))
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

        // POST: api/Tickets
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Ticket>> PostTicket(Ticket ticket)
        {
            try
            {
                await ValidateTicket(ticket);
                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetTicket", new { id = ticket.Id }, ticket);
            }
            catch (TicketValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Tickets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Archivo no válido");

            var filePath = Path.GetTempFileName();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var readExcel = new ReadExcel();
            var tickets = readExcel.ReadExcell(filePath);

            var validationErrors = new List<string>();
            foreach (var ticket in tickets)
            {
                try
                {
                    await ValidateTicket(ticket);
                }
                catch (TicketValidationException ex)
                {
                    validationErrors.Add($"Ticket {ticket.Number}: {ex.Message}");
                    continue;
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(new { Errors = validationErrors });
            }

            await _context.Tickets.AddRangeAsync(tickets);
            await _context.SaveChangesAsync();
            return Ok($"{tickets.Count} registros insertados");
        }

        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
        public class TicketValidationException : Exception
        {
            public TicketValidationException(string message) : base(message) { }
        }

        private async Task ValidateTicket(Ticket ticket)
        {
            // Validación básica
            if (string.IsNullOrEmpty(ticket.Number) || ticket.Date == default || string.IsNullOrEmpty(ticket.Jornada))
            {
                throw new TicketValidationException("Datos del ticket incompletos");
            }

            // Validación de duplicados
            var isDuplicate = await _context.Tickets
                .AnyAsync(e => e.Number == ticket.Number
                              && e.Date.Date == ticket.Date.Date
                              && e.Jornada == ticket.Jornada);

            if (isDuplicate)
            {
                throw new TicketValidationException($"Ya existe un ticket con el número {ticket.Number} para la fecha {ticket.Date.Date:dd/MM/yyyy} y jornada {ticket.Jornada}");
            }
        }
    }
}

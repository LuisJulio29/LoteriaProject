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

        [HttpPost("GetPatronByNumbers")]
        public async Task<ActionResult<IEnumerable<Patron>>> GetPatronByNumbers([FromBody] int[] numbers)
        {
            if (numbers == null || numbers.Length != 10)
            {
                return BadRequest("Se requiere un array de exactamente 10 números.");
            }
            const int CUALQUIER_VALOR = -1; 
            var patrones = await _context.Patrons.ToListAsync();
            var patronesCoincidentes = patrones.Where(patron =>
            {
                for (int i = 0; i < 10; i++)
                {
                    if (numbers[i] != CUALQUIER_VALOR && patron.PatronNumbers[i] != numbers[i])
                    {
                        return false;
                    }
                }return true;}).ToList();
            if (!patronesCoincidentes.Any())
            {
                return NotFound("No se encontraron patrones coincidentes.");
            }
            return Ok(patronesCoincidentes);
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
            var excludedTickets = new[] { "Pick 3", "Pick 4", "Winning", "Evening" };
            var jornadasToSearch = Jornada.ToLower() == "dia" ? new[] { "Dia", "Tarde" } : new[] { Jornada };
            var tickets = await _context.Tickets.Where(t => t.Date.Date == date.Date && jornadasToSearch.Contains(t.Jornada) && !excludedTickets.Contains(t.Loteria)).ToListAsync();
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

        [HttpGet("GetTotalForColumn")]
        public async Task<ActionResult<int[]>> TotalForColumn([FromQuery] DateTime date, [FromQuery] string Jornada)
        {
            var excludedTickets = new[] { "Pick 3", "Pick 4", "Winning", "Evening" };
            var jornadasToSearch = Jornada.ToLower() == "dia" ? new[] { "Dia", "Tarde" } : new[] { Jornada };
            var tickets = await _context.Tickets
                .Where(t => t.Date.Date == date.Date && jornadasToSearch.Contains(t.Jornada) && !excludedTickets.Contains(t.Loteria)).ToListAsync();
            if (!tickets.Any())
            {
                return NotFound($"No se encontraron tickets para la fecha {date.ToShortDateString()} y jornada {Jornada}");
            }
            int maxLength = tickets.Max(t => t.Number.Length);
            int[] columnSums = new int[maxLength];
            foreach (var ticket in tickets)
            {
                for (int i = 0; i < ticket.Number.Length; i++)
                {
                    if (char.IsDigit(ticket.Number[i]))
                    {
                        columnSums[i] += int.Parse(ticket.Number[i].ToString());
                    }
                }
            }
            return Ok(columnSums);
        }

        [HttpGet("GetVoidinDay/{id}")]
        public async Task<ActionResult<List<PatronForVoid>>> VoidinDay(int id)
        {
            // Buscar el patrón de referencia de forma asíncrona
            var referencePatron = await _context.Patrons.FindAsync(id);
            if (referencePatron == null)
            {
                return NotFound($"No se encontró el patrón con ID {id}");
            }
            if (referencePatron.PatronNumbers == null || referencePatron.PatronNumbers.Length == 0)
            {
                return BadRequest("El patrón de referencia no tiene números válidos");
            }
            if (!referencePatron.PatronNumbers.Contains(0))
            {
                return NotFound("No contiene ningún 0 en su Patrón");
            }
            try
            {
                var patrons = await _context.Patrons
                    .Where(p => p.Id != referencePatron.Id).ToListAsync();
                if (!patrons.Any())
                {
                    return NotFound("No hay otros patrones que cumplan con los requisitos de fecha y jornada");
                }
                var zeroIndices = new List<int>();
                for (int i = 0; i < referencePatron.PatronNumbers.Length; i++)
                {
                    if (referencePatron.PatronNumbers[i] == 0)
                    {
                        zeroIndices.Add(i);
                    }
                }
                var matchingPatrons = patrons
                    .Where(p => PatronMatchesAtLeastOneZero(p, referencePatron.PatronNumbers.Length, zeroIndices))
                    .ToList();
                if (!matchingPatrons.Any())
                {
                    return NotFound("No se encontraron otros patrones con al menos un cero coincidente");
                }
                var result = matchingPatrons.Select(p =>
                {
                    var redundanciasList = GenerarListaRedundancias(referencePatron.PatronNumbers, p.PatronNumbers);
                    var indicesCoincidencias = ObtenerIndicesCoincidencias(redundanciasList);
                    return new PatronForVoid
                    {
                        Patron = p,
                        RedundancyNumbers = indicesCoincidencias.ToArray()
                    };
                }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error interno al procesar la solicitud");
            }
        }
        private bool PatronMatchesAtLeastOneZero(Patron patron, int length, List<int> zeroIndices)
        {
            if (patron.PatronNumbers == null || patron.PatronNumbers.Length != length)
            {
                return false;
            }
            foreach (var index in zeroIndices)
            {
                if (index < patron.PatronNumbers.Length && patron.PatronNumbers[index] == 0)
                {
                    return true;
                }
            }
            return false;
        }

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
            var excludedTickets = new[] { "Pick 3", "Pick 4", "Winning", "Evening" };
            var jornadasToSearch = Jornada.ToLower() == "dia" ? new[] { "Dia", "Tarde" } : new[] { Jornada };
            var tickets = await _context.Tickets
                .Where(t => t.Date.Date == date.Date &&
                            jornadasToSearch.Contains(t.Jornada) &&
                            !excludedTickets.Contains(t.Loteria))
                .ToListAsync();

            if (!tickets.Any())
            {
                return NotFound($"No se encontraron tickets válidos para la fecha {date.ToShortDateString()} y jornada {Jornada}");
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

        [HttpPost("CalculateRange")]
        public async Task<ActionResult<IEnumerable<Patron>>> CalculatePatronRange([FromQuery] DateTime dateInit,[FromQuery] string jornadaInit,[FromQuery] DateTime dateFinal,[FromQuery] string jornadaFinal)
        {
            if (dateInit > dateFinal)
            {
                return BadRequest("La fecha inicial no puede ser posterior a la fecha final");
            }
            var results = new List<Patron>();
            var currentDate = dateInit.Date;
            var jornadas = new[] { "Dia", "Noche" };
            bool hasStartedJornadas = false;
            while (currentDate <= dateFinal.Date)
            {
                foreach (var jornada in jornadas)
                {
                    if (currentDate == dateInit.Date && !hasStartedJornadas)
                    {
                        if (jornada.ToLower() == jornadaInit.ToLower())
                        {
                            hasStartedJornadas = true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (currentDate == dateFinal.Date &&
                        hasStartedJornadas &&
                        jornada.ToLower() == jornadaFinal.ToLower())
                    {
                        try
                        {
                            var result = await CalculatePatron(currentDate, jornada);
                            if (result.Result is OkObjectResult okResult)
                            {
                                results.Add((Patron)okResult.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error procesando fecha {currentDate} jornada {jornada}: {ex.Message}");
                        }
                        break;
                    }
                    try
                    {
                        var result = await CalculatePatron(currentDate, jornada);
                        if (result.Result is NotFoundResult)
                        {
                            continue;
                        }
                        if (result.Result is OkObjectResult okResult)
                        {
                            results.Add((Patron)okResult.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error procesando fecha {currentDate} jornada {jornada}: {ex.Message}");
                        continue;
                    }
                }
                currentDate = currentDate.AddDays(1);
            }
            if (!results.Any())
            {
                return NotFound("No se encontraron patrones en el rango de fechas especificado");
            }
            return Ok(results);
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

        [HttpGet("AnalyzePatternRedundancy")]
        public async Task<ActionResult<object>> AnalyzePatternRedundancy([FromQuery] int patron1Id, [FromQuery] int patron2Id)
        {
            var patron1 = await _context.Patrons.FindAsync(patron1Id);
            var patron2 = await _context.Patrons.FindAsync(patron2Id);
            if (patron1 == null || patron2 == null)
            {
                return NotFound("No se encontraron los patrones especificados");
            }
            var redundanciasList = GenerarListaRedundancias(patron1.PatronNumbers, patron2.PatronNumbers);
            var numbersToSearch = ObtenerIndicesCoincidencias(redundanciasList);
            if (!numbersToSearch.Any())
            {
                return NotFound("No se encontraron coincidencias entre los patrones");
            }
            var (fechaBusqueda, jornadasBusqueda) = DeterminarFechaYJornada(patron1);
            var tickets = await _context.Tickets
                .Where(t => t.Date.Date == fechaBusqueda.Date &&
                            jornadasBusqueda.Contains(t.Jornada)).ToListAsync();
            if (!tickets.Any())
            {
                return NotFound($"No se encontraron tickets para la fecha {fechaBusqueda.ToShortDateString()} y jornadas {string.Join(", ", jornadasBusqueda)}");
            }
            var (ticketsWith4Matches, ticketsWith3Matches) = ClasificarTicketsPorCoincidencias(tickets, numbersToSearch);
            return Ok(new
            {
                patron = patron1,
                NumbersToSearch = numbersToSearch,
                TicketsCon4Coincidencias = ticketsWith4Matches,
                TicketsCon3Coincidencias = ticketsWith3Matches
            });
        }
        private int[] GenerarListaRedundancias(int[] patron1Numbers, int[] patron2Numbers)
        {
            var resultado = new int[10];
            for (int i = 0; i < 10; i++)
            {
                if (patron1Numbers[i] == patron2Numbers[i])
                {
                    resultado[i] = patron1Numbers[i];
                }
                else
                {
                    resultado[i] = 99;
                }
            }
            return resultado;
        }
        private List<int> ObtenerIndicesCoincidencias(int[] redundanciasList)
        {
            var indices = new List<int>();
            for (int i = 0; i < redundanciasList.Length; i++)
            {
                if (redundanciasList[i] != 99)
                {
                    indices.Add(i);
                }
            }
            return indices;
        }
        private (DateTime fecha, string[] jornadas) DeterminarFechaYJornada(Patron patron)
        {
            DateTime fecha = patron.Date;
            string[] jornadas;
            if (patron.Jornada.ToLower() == "dia")
            {
                jornadas = new[] { "Noche" };
            }
            else
            {
                jornadas = new[] { "Dia", "Tarde" };
                fecha = fecha.AddDays(1);
            }
            return (fecha, jornadas);
        }
        private (List<Ticket> TicketsWith4Matches, List<Ticket> TicketsWith3Matches) ClasificarTicketsPorCoincidencias(List<Ticket> tickets, List<int> numbersToSearch)
        {
            var ticketsWith4Matches = new List<Ticket>();
            var ticketsWith3Matches = new List<Ticket>();
            foreach (var ticket in tickets)
            {
                int coincidencias = ContarCoincidencias(ticket.Number, numbersToSearch);

                if (coincidencias >= 4)
                {
                    ticketsWith4Matches.Add(ticket);
                }
                else if (coincidencias == 3)
                {
                    ticketsWith3Matches.Add(ticket);
                }
            }
            ticketsWith4Matches = ticketsWith4Matches
                .OrderByDescending(t => ContarCoincidencias(t.Number, numbersToSearch)).ToList();
            return (ticketsWith4Matches, ticketsWith3Matches);
        }
        private int ContarCoincidencias(string ticketNumber, List<int> numbersToSearch)
        {
            int coincidencias = 0;
            foreach (char digitChar in ticketNumber)
            {
                if (char.IsDigit(digitChar))
                {
                    int digit = int.Parse(digitChar.ToString());
                    if (numbersToSearch.Contains(digit))
                    {
                        coincidencias++;
                    }
                }
            }
            return coincidencias;
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

        public class PatronForVoid
        {
            public required Patron Patron { get; set; }
            public required int[] RedundancyNumbers { get; set; }
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

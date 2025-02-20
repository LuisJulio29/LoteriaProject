using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoteriaProject.Context;
using LoteriaProject.Model;
using static LoteriaProject.Controllers.PatronsController;

namespace LoteriaProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SorteoPatronsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SorteoPatronsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/SorteoPatrons
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SorteoPatron>>> GetSorteosPatrons()
        {
            return await _context.SorteosPatrons.ToListAsync();
        }
        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<SorteoPatron>>> SearchSorteosPatrons(DateTime date)
        {
            var sorteoPatrons = await _context.SorteosPatrons.Where(sp => sp.Date.Date == date.Date).ToListAsync();
            if (sorteoPatrons == null || !sorteoPatrons.Any())
            {
                return NotFound();
            }
            return sorteoPatrons;
        }
        [HttpGet("CalculateRedundancy")]
        public async Task<ActionResult<List<SorteoPatronRedundancy>>> CalculateRedundancy([FromBody] SorteoPatron sorteoPatron)
        {
            var allSorteoPatrons = await _context.SorteosPatrons.Where(sp => sp.Id != sorteoPatron.Id).ToListAsync();
            var redundancyList = new List<SorteoPatronRedundancy>();
            foreach (var sorteo in allSorteoPatrons)
            {
                int matchCount = 0;
                for (int i = 0; i < 10; i++)
                {
                    if (sorteoPatron.PatronNumbers[i] == sorteo.PatronNumbers[i])
                    {
                        matchCount++;
                    }
                }
                if (matchCount > 3)
                {
                    redundancyList.Add(new SorteoPatronRedundancy
                    {
                        SorteoPatron = sorteo,
                        RedundancyCount = matchCount
                    });
                }
            }
            return Ok(redundancyList.OrderByDescending(r => r.RedundancyCount).ToList());
        }
        [HttpGet("GetRedundancyinDate")]
        public async Task<ActionResult<List<SorteoPatron>>> RedundancyinDate(DateTime date)
        {
            var sorteoPatrons = await _context.SorteosPatrons.Where(sp => sp.Date.Day == date.Day && sp.Date.DayOfWeek == date.DayOfWeek && sp.Date.Date != date.Date).ToListAsync();
            if (sorteoPatrons == null || !sorteoPatrons.Any())
            {
                return NotFound();
            }
            return Ok(sorteoPatrons);
        }
        [HttpGet("GetNumbersNotPlayed")]
        public async Task<ActionResult<string[]>> NumberNotPlayed([FromQuery] DateTime date)
        {
            var sorteos = await _context.Sorteos.Where( s => s.Date.Date == date.Date).ToListAsync();
            if (sorteos == null || !sorteos.Any())
            {
                return NotFound("No se encontraron Sorteos");
            }
            bool[] usedRow1 = new bool[10];
            bool[] usedRow2 = new bool[10];
            bool[] usedRow3 = new bool[10];
            bool[] usedRow4 = new bool[10];
            foreach (var sorteo in sorteos)
            {
                if (sorteo.Number.Length == 4)
                {
                    usedRow1[int.Parse(sorteo.Number[0].ToString())] = true;
                    usedRow2[int.Parse(sorteo.Number[1].ToString())] = true;
                    usedRow3[int.Parse(sorteo.Number[2].ToString())] = true;
                    usedRow4[int.Parse(sorteo.Number[3].ToString())] = true;
                }
            }
            List<int> notUsedRow1 = GetNotUsedNumbers(usedRow1);
            List<int> notUsedRow2 = GetNotUsedNumbers(usedRow2);
            List<int> notUsedRow3 = GetNotUsedNumbers(usedRow3);
            List<int> notUsedRow4 = GetNotUsedNumbers(usedRow4);
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
        public async Task<ActionResult<int[]>> TotalForColumn([FromQuery] DateTime date)
        {
            var Sorteos = await _context.Sorteos.Where(s => s.Date.Date == date.Date).ToListAsync();
            if (!Sorteos.Any())
            {
                return NotFound($"No se encontraron Sorteos para la fecha {date.ToShortDateString()}");
            }
            int maxLength = Sorteos.Max(t => t.Number.Length);
            int[] columnSums = new int[maxLength];
            foreach (var sorteo in Sorteos)
            {
                for (int i = 0; i < sorteo.Number.Length; i++)
                {
                    if (char.IsDigit(sorteo.Number[i]))
                    {
                        columnSums[i] += int.Parse(sorteo.Number[i].ToString());
                    }
                }
            }
            return Ok(columnSums);
        }
        [HttpGet("GetVoidinDay/{id}")]
        public async Task<ActionResult<List<Patron>>> VoidinDay(int id)
        {
            var SorteoPatron = await _context.SorteosPatrons.FindAsync(id);
            if (SorteoPatron == null)
            {
                return NotFound($"No se encontró el patrón con ID {id}");
            }
            // Validar que PatronNumbers no sea null
            if (SorteoPatron.PatronNumbers == null || SorteoPatron.PatronNumbers.Length == 0)
            {
                return BadRequest("El patrón de referencia no tiene números válidos");
            }
            if (!SorteoPatron.PatronNumbers.Contains(0))
            {
                return NotFound("No contiene ningun 0 en su Patron");
            }
            try
            {
                // Obtener todos los patrones que coincidan con el día y jornada, excluyendo el patrón de referencia
                var Spatrons = await _context.SorteosPatrons
                    .Where(p => p.Id != SorteoPatron.Id && p.Date.Day == SorteoPatron.Date.Day).ToListAsync();
                if (!Spatrons.Any())
                {
                    return NotFound("No hay otros patrones que cumplan con los requisitos de fecha y jornada");
                }
                // Obtener índices de ceros del patrón de referencia
                var zeroIndices = new HashSet<int>(); // Usando HashSet para búsquedas más eficientes
                for (int i = 0; i < SorteoPatron.PatronNumbers.Length; i++)
                {
                    if (SorteoPatron.PatronNumbers[i] == 0)
                    {
                        zeroIndices.Add(i);
                    }
                }
                // Filtrar los patrones que coincidan en los mismos ceros
                var matchingPatrons = Spatrons
                    .Where(p => PatronMatchesZeroPattern(p, SorteoPatron.PatronNumbers.Length, zeroIndices))
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

        // GET: api/SorteoPatrons/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SorteoPatron>> GetSorteoPatron(int id)
        {
            var sorteoPatron = await _context.SorteosPatrons.FindAsync(id);

            if (sorteoPatron == null)
            {
                return NotFound();
            }

            return sorteoPatron;
        }

        // PUT: api/SorteoPatrons/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSorteoPatron(int id, SorteoPatron sorteoPatron)
        {
            if (id != sorteoPatron.Id)
            {
                return BadRequest();
            }

            _context.Entry(sorteoPatron).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SorteoPatronExists(id))
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

        // POST: api/SorteoPatrons
        [HttpPost]
        public async Task<ActionResult<SorteoPatron>> PostSorteoPatron(SorteoPatron sorteoPatron)
        {
            try
            {
                await ValidateSorteoPatron(sorteoPatron);
                _context.SorteosPatrons.Add(sorteoPatron);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetSorteoPatron", new { id = sorteoPatron.Id }, sorteoPatron);
            }
            catch (PatronValidationException ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost("Calculate")]
        public async Task<ActionResult<SorteoPatron>> CalculateSorteoPatron([FromQuery] DateTime date)
        {
            var Sorteos = await _context.Sorteos.Where(t => t.Date.Date == date.Date).ToListAsync();

            if (!Sorteos.Any())
            {
                return NotFound($"No se encontraron Sorteos válidos para la fecha {date.ToShortDateString()}");
            }
            int[] patronNumbers = new int[10];
            foreach (var sorteo in Sorteos)
            {
                foreach (char digit in sorteo.Number)
                {
                    if (int.TryParse(digit.ToString(), out int number))
                    {
                        patronNumbers[number]++;
                    }
                }
            }
            var SorteoPatron = new SorteoPatron
            {
                Date = date.Date,
                PatronNumbers = patronNumbers
            };

            try
            {
                await ValidateSorteoPatron(SorteoPatron);
                _context.SorteosPatrons.Add(SorteoPatron);
                await _context.SaveChangesAsync();
                return Ok(SorteoPatron);
            }
            catch (PatronValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("CalculateRange")]
        public async Task<ActionResult<IEnumerable<SorteoPatron>>> CalculateSorteoPatronRange([FromQuery] DateTime dateInit,[FromQuery] DateTime dateFinal)
        {
            if (dateInit > dateFinal)
            {
                return BadRequest("La fecha inicial no puede ser posterior a la fecha final");
            }

            var results = new List<SorteoPatron>();
            var currentDate = dateInit.Date;

            while (currentDate <= dateFinal.Date)
            {
                try
                {
                    var result = await CalculateSorteoPatron(currentDate);
                    if (result.Result is OkObjectResult okResult)
                    {
                        results.Add((SorteoPatron)okResult.Value);
                    }
                    else if (result.Result is NotFoundResult)
                    {
                        // Log that no SorteoPatron was found for this date
                        Console.WriteLine($"No se encontró SorteoPatron para la fecha {currentDate:yyyy-MM-dd}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error procesando fecha {currentDate:yyyy-MM-dd}: {ex.Message}");
                }

                currentDate = currentDate.AddDays(1);
            }

            if (!results.Any())
            {
                return NotFound("No se encontraron patrones en el rango de fechas especificado");
            }

            return Ok(results);
        }
        // DELETE: api/SorteoPatrons/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSorteoPatron(int id)
        {
            var sorteoPatron = await _context.SorteosPatrons.FindAsync(id);
            if (sorteoPatron == null)
            {
                return NotFound();
            }

            _context.SorteosPatrons.Remove(sorteoPatron);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SorteoPatronExists(int id)
        {
            return _context.SorteosPatrons.Any(e => e.Id == id);
        }
        public class SorteoPatronRedundancy
        {
            public required SorteoPatron SorteoPatron { get; set; }
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
        private static bool PatronMatchesZeroPattern(SorteoPatron patron, int referenceLength, HashSet<int> zeroIndices)
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

        private async Task ValidateSorteoPatron(SorteoPatron patron)
        {
            if (patron.PatronNumbers == null || patron.PatronNumbers.Length != 10)
            {
                throw new PatronValidationException("El patrón debe contener exactamente 10 números");
            }

            var existingPatron = await _context.Patrons
                .FirstOrDefaultAsync(p => p.Date.Date == patron.Date.Date);

            if (existingPatron != null)
            {
                if (patron.PatronNumbers.SequenceEqual(existingPatron.PatronNumbers))
                {
                    throw new PatronValidationException($"Ya existe un patrón idéntico para la fecha {patron.Date.Date:dd/MM/yyyy}");
                }
            }
        }
    }
}

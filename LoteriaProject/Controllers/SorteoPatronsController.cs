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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SorteoPatron>> PostSorteoPatron(SorteoPatron sorteoPatron)
        {
            _context.SorteosPatrons.Add(sorteoPatron);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSorteoPatron", new { id = sorteoPatron.Id }, sorteoPatron);
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
    }
}

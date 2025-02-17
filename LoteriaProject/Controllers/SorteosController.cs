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
            _context.Sorteos.Add(sorteo);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSorteo", new { id = sorteo.Id }, sorteo);
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

        private bool SorteoExists(int id)
        {
            return _context.Sorteos.Any(e => e.Id == id);
        }
    }
}

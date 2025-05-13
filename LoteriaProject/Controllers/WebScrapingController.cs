using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using LoteriaProject.Context;
using LoteriaProject.Model;
using LoteriaProject.Custom;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;

namespace LoteriaProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebScrapingController : ControllerBase
    {
        private readonly ILogger<WebScrapingController> _logger;
        private readonly AppDbContext _context;

        public WebScrapingController(ILogger<WebScrapingController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Obtiene los resultados de loterías directamente desde la web sin guardarlos en la base de datos
        /// </summary>
        [HttpGet("ScrapeLoterias")]
        public async Task<ActionResult<IEnumerable<Ticket>>> ScrapeLoterias()
        {
            try
            {
                var tickets = await ScrapeLoteriasDeHoy(_logger);
                if (tickets == null || !tickets.Any())
                {
                    return NotFound("No se encontraron resultados de loterías.");
                }
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar web scraping");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene los resultados de loterías desde la web y los guarda en la base de datos
        /// </summary>
        [HttpGet("ScrapeAndSaveTickets")]
        public async Task<ActionResult<ScrapingResult>> ScrapeAndSaveTickets()
        {
            try
            {
                var tickets = await ScrapeLoteriasDeHoy(_logger);
                if (tickets == null || !tickets.Any())
                {
                    return NotFound("No se encontraron resultados de loterías.");
                }

                var result = new ScrapingResult
                {
                    TotalTickets = tickets.Count,
                    SavedTickets = 0,
                    SkippedTickets = 0,
                    Errors = new List<string>()
                };

                foreach (var ticket in tickets)
                {
                    try
                    {
                        await ValidateTicket(ticket);

                        // Si pasa validación, guardar en la base de datos
                        _context.Tickets.Add(ticket);
                        await _context.SaveChangesAsync();
                        result.SavedTickets++;
                    }
                    catch (TicketValidationException ex)
                    {
                        _logger.LogWarning($"Ticket omitido: {ex.Message}. Lotería: {ticket.Loteria}, Fecha: {ticket.Date:dd/MM/yyyy}, Jornada: {ticket.Jornada}, Número: {ticket.Number}");
                        result.SkippedTickets++;
                        result.Errors.Add($"Lotería: {ticket.Loteria}, Fecha: {ticket.Date:dd/MM/yyyy}, Jornada: {ticket.Jornada}, Número: {ticket.Number} - Error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al guardar ticket. Lotería: {ticket.Loteria}, Fecha: {ticket.Date:dd/MM/yyyy}, Jornada: {ticket.Jornada}, Número: {ticket.Number}");
                        result.SkippedTickets++;
                        result.Errors.Add($"Lotería: {ticket.Loteria}, Fecha: {ticket.Date:dd/MM/yyyy}, Jornada: {ticket.Jornada}, Número: {ticket.Number} - Error inesperado: {ex.Message}");
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar web scraping o guardar datos");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Endpoint para guardar un ticket individualmente
        /// </summary>
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

        /// <summary>
        /// Valida un ticket antes de guardarlo en la base de datos
        /// </summary>
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
                              && e.Jornada == ticket.Jornada
                              && e.Loteria == ticket.Loteria);
            if (isDuplicate)
            {
                throw new TicketValidationException($"Ya existe un ticket con el número {ticket.Number} para la loteria {ticket.Loteria} en la fecha {ticket.Date.Date:dd/MM/yyyy} y jornada {ticket.Jornada}");
            }

            // Validación adicional para el caso de Astro
            if (ticket.Loteria.Equals("Astro", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(ticket.sign))
            {
                throw new TicketValidationException("El signo zodiacal es obligatorio para la lotería Astro");
            }

            // Validación de formato de número según tipo de lotería (se puede extender si es necesario)
            if (!ValidateNumberFormat(ticket))
            {
                throw new TicketValidationException($"El formato del número '{ticket.Number}' no es válido para la lotería {ticket.Loteria}");
            }
        }

        /// <summary>
        /// Valida el formato del número según el tipo de lotería
        /// </summary>
        private bool ValidateNumberFormat(Ticket ticket)
        {
            // Esta validación se puede ampliar según las reglas específicas de cada lotería
            if (string.IsNullOrEmpty(ticket.Number))
                return false;

            // Para Astro, validar que sea numérico y tenga 4 dígitos
            if (ticket.Loteria.Equals("Astro", StringComparison.OrdinalIgnoreCase))
            {
                return ticket.Number.Length == 4 && ticket.Number.All(char.IsDigit);
            }

            // Validación general para otras loterías (solo dígitos)
            return ticket.Number.All(char.IsDigit);
        }

        public static async Task<List<Ticket>> ScrapeLoteriasDeHoy(ILogger _logger)
        {
            var tickets = new List<Ticket>();
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://loteriasdehoy.co/");
            var html = await response.Content.ReadAsStringAsync();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            // Seleccionar todos los divs con clase "chances_hoy"
            var chancesNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='chances_hoy']");

            if (chancesNodes != null)
            {
                int id = 1;
                foreach (var chanceNode in chancesNodes)
                {
                    try
                    {
                        // Obtener nombre de la lotería
                        var loteriaNode = chanceNode.SelectSingleNode(".//h3/a");
                        if (loteriaNode == null) continue;

                        string loteriaCompleta = loteriaNode.InnerText.Trim();

                        // Separar lotería y jornada
                        string loteria = loteriaCompleta;
                        string jornada = "";
                        string signoZodiaco = "";

                        // Procesar nombre de lotería - casos especiales primero
                        ProcessLoteryName(ref loteria, ref jornada, loteriaCompleta);

                        // Obtener fecha
                        var fechaNode = chanceNode.SelectSingleNode(".//div[@class='fecha_resultado']");
                        if (fechaNode == null) continue;

                        string fechaText = fechaNode.InnerText.Trim();
                        DateTime fecha;

                        try
                        {
                            // Intentar parsear con formato español
                            fecha = DateTime.ParseExact(fechaText, "dd MMMM yyyy", new CultureInfo("es-ES"));
                        }
                        catch
                        {
                            // Si falla, intentar con un formato alternativo
                            fecha = Convert.ToDateTime(fechaText, new CultureInfo("es-ES"));
                        }
                        // --- INICIO: Validación específica de Jornada ---
                        // 2. Si la jornada sigue vacía después de ProcessLoteryName, aplica reglas especiales
                        if (string.IsNullOrEmpty(jornada))
                        {
                            bool isHoliday = ColombianHolidays.IsHoliday(fecha);
                            DayOfWeek dayOfWeek = fecha.DayOfWeek;

                            // Reglas para Saman
                            if (loteria.Equals("Saman", StringComparison.OrdinalIgnoreCase) ||
                                loteria.Equals("Samán", StringComparison.OrdinalIgnoreCase)) // Considera acento
                            {
                                if (dayOfWeek == DayOfWeek.Sunday || isHoliday)
                                {
                                    jornada = "Noche";
                                }
                                else // Lunes a Sábado (y no festivo)
                                {
                                    jornada = "Dia";
                                }
                                _logger?.LogInformation($"Regla específica aplicada: Samán/Saman ({fecha.ToShortDateString()} DoW:{dayOfWeek} Hol:{isHoliday}) -> Jornada: {jornada}");
                            }
                            // Reglas para Pijao De Oro
                            else if (loteria.Equals("Pijao De Oro", StringComparison.OrdinalIgnoreCase))
                            {
                                if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday || isHoliday)
                                {
                                    jornada = "Noche";
                                }
                                else // Lunes a Viernes (y no festivo)
                                {
                                    jornada = "Dia";
                                }
                                _logger?.LogInformation($"Regla específica aplicada: Pijao De Oro ({fecha.ToShortDateString()} DoW:{dayOfWeek} Hol:{isHoliday}) -> Jornada: {jornada}");
                            }
                            // Puedes añadir más 'else if' para otras loterías con reglas especiales aquí
                        }

                        // 3. Si AÚN sigue vacía, podría ser un sorteo único o un error.
                        if (string.IsNullOrEmpty(jornada))
                        {
                            // Decide qué hacer: asignar "Unica", dejar vacío, o registrar advertencia
                            _logger?.LogWarning($"La jornada para '{loteriaCompleta}' del {fecha.ToShortDateString()} no pudo ser determinada por sufijo ni regla específica.");
                            // jornada = "Unica"; // Opción: Asignar un valor por defecto si aplica
                        }
                        // --- FIN: Validación específica de Jornada ---
                        // Obtener los nodos span para el número
                        var resultadoNodes = chanceNode.SelectNodes(".//div[@class='resultado_chances_hoy']/span");
                        // Validar si se encontraron los nodos span
                        if (resultadoNodes == null || resultadoNodes.Count == 0)
                        {
                            _logger.LogWarning($"No se encontraron spans de resultado para {loteriaCompleta} en la fecha {fechaText}");
                            continue; // Saltar este resultado si no hay spans
                        }

                        // Construir el número solo con el contenido de los spans
                        string numero = string.Join("", resultadoNodes.Select(n => n.InnerText.Trim()));

                        string sign = ""; // Inicializar vacío

                        // Manejar caso especial para Astro (extraer signo zodiacal)
                        // Puedes añadir otros nombres si es necesario, como "Super Astro"
                        if (loteria.Equals("Astro", StringComparison.OrdinalIgnoreCase))
                        {
                            // Obtener el nodo div que contiene todo el resultado (números y signo)
                            var resultadoDivNode = chanceNode.SelectSingleNode(".//div[@class='resultado_chances_hoy']");
                            if (resultadoDivNode != null)
                            {
                                // Obtener TODO el texto interno del div, decodificado por si hay entidades HTML
                                string fullResultText = System.Net.WebUtility.HtmlDecode(resultadoDivNode.InnerText.Trim());

                                // Buscar el ÚLTIMO guión en el texto completo
                                int separatorIndex = fullResultText.LastIndexOf('-');

                                // Verificar si se encontró un guión y si hay texto después de él
                                if (separatorIndex >= 0 && separatorIndex < fullResultText.Length - 1)
                                {
                                    // Extraer la subcadena DESPUÉS del último guión y limpiarla
                                    signoZodiaco = fullResultText.Substring(separatorIndex + 1).Trim();

                                    // **Opcional pero recomendado: Validar si es un signo conocido**
                                    string[] signosValidos = {
                                        "Aries", "Tauro", "Géminis", "Geminis", "Cáncer", "Cancer", "Leo", "Virgo",
                                        "Libra", "Escorpio", "Sagitario", "Capricornio", "Acuario", "Piscis"
                                    };
                                    // Comprobar si el texto extraído está en la lista de signos válidos (ignorando mayúsculas/minúsculas)
                                    if (!signosValidos.Contains(signoZodiaco, StringComparer.OrdinalIgnoreCase))
                                    {
                                        _logger.LogWarning($"Texto '{signoZodiaco}' extraído para Astro no parece ser un signo zodiacal válido. Lotería: {loteriaCompleta}, Texto completo: '{fullResultText}'");
                                        signoZodiaco = ""; // Si no es válido, dejarlo vacío o manejar el error como prefieras
                                    }
                                }
                                else
                                {
                                    // Registrar si no se encontró el patrón esperado para Astro
                                    _logger.LogWarning($"No se encontró el patrón de signo ('- Signo') para Astro. Lotería: {loteriaCompleta}, Texto completo: '{fullResultText}'");
                                }
                            }
                            else
                            {
                                _logger.LogWarning($"No se encontró el div 'resultado_chances_hoy' para Astro. Lotería: {loteriaCompleta}");
                            }
                        }

                        // Crear y añadir el ticket
                        var ticket = new Ticket
                        {
                            Loteria = loteria,
                            Jornada = jornada,
                            Date = fecha,
                            Number = numero,      // 'numero' ahora SÓLO contiene los dígitos
                            sign = signoZodiaco   // 'signoZodiaco' contiene el signo extraído o está vacío
                        };

                        tickets.Add(ticket);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al procesar un nodo: {ex.Message}");
                        continue;
                    }
                }
            }

            return tickets;
        }

        private static void ProcessLoteryName(ref string loteria, ref string jornada, string loteriaCompleta)
        {
            if (loteriaCompleta.EndsWith("Noche"))
            {
                loteria = Regex.Replace(loteriaCompleta, "\\s+Noche$", "").Trim();
                jornada = "Noche";
            }
            else if (loteriaCompleta.EndsWith("Tarde"))
            {
                loteria = Regex.Replace(loteriaCompleta, "\\s+Tarde$", "").Trim();
                jornada = "Tarde";
            }
            else if (loteriaCompleta.EndsWith("Mañana"))
            {
                loteria = Regex.Replace(loteriaCompleta, "\\s+Mañana$", "").Trim();
                jornada = "Dia";
            }
            else if (loteriaCompleta.EndsWith("Día"))
            {
                loteria = Regex.Replace(loteriaCompleta, "\\s+Día$", "").Trim();
                jornada = "Dia";
            }
            else if (loteriaCompleta.EndsWith("1"))
            {
                loteria = Regex.Replace(loteriaCompleta, "\\s+1$", "").Trim();
                jornada = "Dia";
            }
            else if (loteriaCompleta.EndsWith("2"))
            {
                loteria = Regex.Replace(loteriaCompleta, "\\s+2$", "").Trim();
                jornada = "Tarde";
            }
            else if (loteriaCompleta.EndsWith("3"))
            {
                loteria = loteriaCompleta.Trim();
                jornada = "Noche";
            }
            else if (loteriaCompleta.EndsWith("Sol"))
            {
                loteria = Regex.Replace(loteriaCompleta, "\\s+Sol$", "").Trim();
                jornada = "Dia";
            }
            else if (loteriaCompleta.EndsWith("Luna"))
            {
                loteria = Regex.Replace(loteriaCompleta, "\\s+Luna$", "").Trim();
                jornada = "Noche";
            }
        }
    }

    /// <summary>
    /// Clase para representar el resultado del proceso de scraping y guardado
    /// </summary>
    public class ScrapingResult
    {
        public int TotalTickets { get; set; }
        public int SavedTickets { get; set; }
        public int SkippedTickets { get; set; }
        public List<string> Errors { get; set; }
    }

    /// <summary>
    /// Excepción personalizada para validación de tickets
    /// </summary>
    public class TicketValidationException : Exception
    {
        public TicketValidationException(string message) : base(message)
        {
        }
    }
}
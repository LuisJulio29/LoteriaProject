using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LoteriaProject.Controllers;

namespace LoteriaProject.Custom
{
    public class LoteriasSchedulerService : BackgroundService
    {
        private readonly ILogger<LoteriasSchedulerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeZoneInfo _colombiaTimeZone;

        public LoteriasSchedulerService(
            ILogger<LoteriasSchedulerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            try
            {
                // Definir la zona horaria de Colombia (UTC-5)
                // Intentar primero con el ID estándar de Windows
                _colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch
            {
                try
                {
                    // Si falla (por ejemplo, en Linux), intentar con el ID de Linux
                    _colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                }
                catch
                {
                    // Si todo falla, crear una zona horaria personalizada
                    _colombiaTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "Colombia Time",
                        new TimeSpan(-5, 0, 0),
                        "Colombia Time",
                        "Colombia Standard Time");
                }
            }

            _logger.LogInformation("Servicio de programación de loterías iniciado correctamente.");
            _logger.LogInformation($"Zona horaria configurada: {_colombiaTimeZone.DisplayName}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de programación de loterías está ejecutándose.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Obtener la hora actual en Colombia
                    var colombiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _colombiaTimeZone);
                    _logger.LogDebug($"Hora actual en Colombia: {colombiaTime:yyyy-MM-dd HH:mm:ss}");

                    // Calcular la hora de la próxima ejecución (4:05 PM o 11:10 PM)
                    var nextRun = GetNextRunTime(colombiaTime);
                    var timeToWait = nextRun - colombiaTime;

                    // Si el tiempo de espera es negativo (la hora ya pasó hoy), ajustar al día siguiente
                    if (timeToWait.TotalMilliseconds <= 0)
                    {
                        nextRun = nextRun.AddDays(1);
                        timeToWait = nextRun - colombiaTime;
                    }

                    _logger.LogInformation($"Próxima ejecución programada para: {nextRun:yyyy-MM-dd HH:mm:ss} (en {timeToWait.TotalHours:F2} horas)");

                    // Esperar hasta la próxima ejecución
                    await Task.Delay(timeToWait, stoppingToken);

                    // Si la aplicación sigue en ejecución después del período de espera, ejecutar la tarea
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await RunScheduledTaskAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Registrar cualquier error, pero continuar ejecutándose
                    _logger.LogError(ex, "Error en el servicio de programación de loterías");

                    // Esperar un tiempo de recuperación antes de intentar de nuevo
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private DateTime GetNextRunTime(DateTime currentTime)
        {
            // Definir las horas programadas (4:05 PM y 11:10 PM)
            var afternoon = new TimeSpan(16, 5, 0); // 16:05 (4:05 PM)
            var night = new TimeSpan(23, 10, 0);    // 23:10 (11:10 PM)

            // Hora actual
            var currentTimeOfDay = currentTime.TimeOfDay;

            // Determinar la próxima ejecución
            if (currentTimeOfDay < afternoon)
            {
                // Si es antes de las 4:05 PM, programar para hoy a las 4:05 PM
                return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 16, 5, 0);
            }
            else if (currentTimeOfDay < night)
            {
                // Si es después de las 4:05 PM pero antes de las 11:10 PM, programar para hoy a las 11:10 PM
                return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 23, 10, 0);
            }
            else
            {
                // Si es después de las 11:10 PM, programar para mañana a las 4:05 PM
                return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 16, 5, 0).AddDays(1);
            }
        }

        private async Task RunScheduledTaskAsync()
        {
            _logger.LogInformation("Iniciando ejecución programada de scraping de loterías");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Obtener una instancia del controlador
                    var controller = scope.ServiceProvider.GetRequiredService<WebScrapingController>();

                    // Ejecutar el método directamente
                    var result = await controller.ScrapeAndSaveTickets();

                    // Registrar el resultado
                    if (result.Value != null)
                    {
                        var scrapingResult = result.Value;
                        _logger.LogInformation($"Scraping completado. Total: {scrapingResult.TotalTickets}, " +
                                              $"Guardados: {scrapingResult.SavedTickets}, " +
                                              $"Omitidos: {scrapingResult.SkippedTickets}");

                        // Registrar errores si hay alguno
                        if (scrapingResult.Errors != null && scrapingResult.Errors.Count > 0)
                        {
                            _logger.LogWarning($"Se encontraron {scrapingResult.Errors.Count} errores durante el scraping:");
                            foreach (var error in scrapingResult.Errors)
                            {
                                _logger.LogWarning($"- {error}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("El scraping programado no retornó resultados.");
                    }
                }

                _logger.LogInformation("Ejecución programada completada con éxito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar la tarea programada de scraping");
            }
        }
    }
}
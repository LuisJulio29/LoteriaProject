namespace LoteriaProject.Custom
{
    public class ColombianHolidays
    {

        // Calcula el Domingo de Pascua para un año dado usando el algoritmo de Meeus/Jones/Butcher
        private static DateTime GetEasterSunday(int year)
        {
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;
            return new DateTime(year, month, day);
        }

        // Mueve una fecha al siguiente lunes si no cae en lunes
        private static DateTime MoveToNextMonday(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Monday)
                return date;
            int daysToAdd = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
            // Si daysToAdd es 0 significa que ya es lunes, pero el chequeo inicial lo maneja.
            // Si es domingo (0), daysToAdd = (1-0+7)%7 = 1. Correcto.
            // Si es sábado (6), daysToAdd = (1-6+7)%7 = 2. Correcto.
            return date.AddDays(daysToAdd);
        }

        private static readonly HashSet<DateTime> _holidaysCache = new HashSet<DateTime>();
        private static int _cachedYear = -1;

        // Obtiene la lista de festivos para un año específico
        private static HashSet<DateTime> GetHolidaysForYear(int year)
        {
            // Cache simple para evitar recalcular para el mismo año repetidamente
            if (year == _cachedYear)
            {
                return _holidaysCache;
            }

            var holidays = new HashSet<DateTime>();
            DateTime easterSunday = GetEasterSunday(year);

            // Festivos Fijos
            holidays.Add(new DateTime(year, 1, 1));   // Año Nuevo
            holidays.Add(new DateTime(year, 5, 1));   // Día del Trabajo
            holidays.Add(new DateTime(year, 7, 20));  // Independencia Colombia
            holidays.Add(new DateTime(year, 8, 7));   // Batalla de Boyacá
            holidays.Add(new DateTime(year, 12, 8));  // Inmaculada Concepción
            holidays.Add(new DateTime(year, 12, 25)); // Navidad

            // Festivos Basados en Pascua (no se mueven)
            holidays.Add(easterSunday.AddDays(-3)); // Jueves Santo
            holidays.Add(easterSunday.AddDays(-2)); // Viernes Santo

            // Festivos que se mueven al siguiente lunes (Ley Emiliani)
            holidays.Add(MoveToNextMonday(new DateTime(year, 1, 6)));    // Reyes Magos
            holidays.Add(MoveToNextMonday(new DateTime(year, 3, 19)));   // San José
            holidays.Add(MoveToNextMonday(new DateTime(year, 6, 29)));   // San Pedro y San Pablo
            holidays.Add(MoveToNextMonday(new DateTime(year, 8, 15)));   // Asunción de la Virgen
            holidays.Add(MoveToNextMonday(new DateTime(year, 10, 12)));  // Día de la Raza
            holidays.Add(MoveToNextMonday(new DateTime(year, 11, 1)));   // Todos los Santos
            holidays.Add(MoveToNextMonday(new DateTime(year, 11, 11)));  // Independencia de Cartagena

            // Festivos basados en Pascua que se mueven al lunes
            holidays.Add(MoveToNextMonday(easterSunday.AddDays(40))); // Ascensión del Señor
            holidays.Add(MoveToNextMonday(easterSunday.AddDays(61))); // Corpus Christi
            holidays.Add(MoveToNextMonday(easterSunday.AddDays(68))); // Sagrado Corazón de Jesús

            // Actualizar cache
            _holidaysCache.Clear();
            foreach (var holiday in holidays)
            {
                _holidaysCache.Add(holiday);
            }
            _cachedYear = year;

            return _holidaysCache;
        }

        /// <summary>
        /// Verifica si una fecha dada es un festivo oficial en Colombia.
        /// </summary>
        /// <param name="date">La fecha a verificar.</param>
        /// <returns>True si es festivo, False en caso contrario.</returns>
        public static bool IsHoliday(DateTime date)
        {
            // Comparamos solo la parte de la fecha, ignorando la hora
            DateTime dateOnly = date.Date;
            int year = dateOnly.Year;
            var holidays = GetHolidaysForYear(year);
            return holidays.Contains(dateOnly);
        }
    }
}

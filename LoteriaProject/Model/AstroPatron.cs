namespace LoteriaProject.Model
{
    public class AstroPatron
    {
        public int Id { get; set; }
        public required DateTime Date { get; set; }
        public required string Jornada { get; set; }
        public required AstroSign[] Sign { get; set; }
        public required int[] Row1 { get; set; }
        public required int[] Row2 { get; set; }
        public required int[] Row3 { get; set; }
        public required int[] Row4 { get; set; }

    }
    public enum AstroSign
    {
        Aries = 1,
        Tauro = 2,
        Geminis = 3,
        Cancer = 4,
        Leo = 5,
        Virgo = 6,
        Libra = 7,
        Escorpio = 8,
        Sagitario = 9,
        Capricornio = 10,
        Acuario = 11,
        Piscis = 12
    }
}

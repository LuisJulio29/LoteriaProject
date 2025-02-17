using System.ComponentModel.DataAnnotations;

namespace LoteriaProject.Model
{
    public class Sorteo
    {
        public int Id { get; set; }
        [MaxLength(4), MinLength(4)]
        public required string Number { get; set; }
        [MaxLength(3), MinLength(3)]
        public required string Serie { get; set; }
        public required DateTime Date { get; set; }
        public required string Loteria { get; set; }
        public required string Jornada { get; set; }
    }
}

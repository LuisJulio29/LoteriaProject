using System.ComponentModel.DataAnnotations;

namespace LoteriaProject.Model
{
    public class Ticket
    {
        public int Id { get; set; }
        [ MaxLength(4), MinLength(4)]
        public required string Number { get; set; }
        public string? sign { get; set; }
        public required DateTime Date { get; set; }
        public required string Loteria { get; set; }
        public required string Jornada { get; set; }

    }
}

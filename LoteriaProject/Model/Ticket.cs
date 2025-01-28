namespace LoteriaProject.Model
{
    public class Ticket
    {
        public int Id { get; set; }
        public required string Number { get; set; }
        public required string Date { get; set; }
        public required string Loteria { get; set; }
        public required string Jornada { get; set; }

    }
}

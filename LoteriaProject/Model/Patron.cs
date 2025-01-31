namespace LoteriaProject.Model
{
    public class Patron
    {
        public int Id { get; set; }
        public required DateTime Date { get; set; }
        public required string Jornada { get; set; }
        public required int[] PatronNumbers { get; set; }
    }
}

namespace LoteriaProject.Model
{
    public class SorteoPatron
    {
        public int Id { get; set; }
        public required DateTime Date { get; set; }
        public required int[] PatronNumbers { get; set; }
    }
}

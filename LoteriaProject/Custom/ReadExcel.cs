using LoteriaProject.Model;
using OfficeOpenXml;

namespace LoteriaProject.Custom
{
    public class ReadExcel
    {
        private string FormatText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return string.Join(" ",
                text.Split(' ')
                .Select(word => word.Length > 0
                    ? char.ToUpper(word[0]) + word.Substring(1).ToLower()
                    : string.Empty)
                .Where(word => !string.IsNullOrEmpty(word)));
        }

        public List<Ticket> ReadExcell(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var tickets = new List<Ticket>();
            FileInfo fileInfo = new FileInfo(filePath);

            using (var package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets["Hoja1"];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var ticket = new Ticket
                    {
                        Number = worksheet.Cells[row, 2].Text.Trim(),
                        Loteria = FormatText(worksheet.Cells[row, 3].Text.Trim()),
                        sign = FormatText(worksheet.Cells[row, 4].Text.Trim()),
                        Jornada = FormatText(worksheet.Cells[row, 5].Text.Trim()),
                        Date = DateTime.Parse(worksheet.Cells[row, 6].Text.Trim())
                    };

                    if (ticket.Number.Length != 4)
                        throw new ArgumentException($"Número inválido en fila {row}");

                    tickets.Add(ticket);
                }
            }
            return tickets;
        }
    }
}
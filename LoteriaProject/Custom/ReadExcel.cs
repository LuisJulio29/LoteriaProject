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

        private void ValidateTicketNumber(string number, string loteria, int row)
        {
            if (loteria == "Pick 3")
            {
                if (number.Length != 3)
                    throw new ArgumentException($"Para Pick 3, el número debe tener 3 dígitos. Error en fila {row}");
            }
            else
            {
                if (number.Length != 4)
                    throw new ArgumentException($"El número debe tener 4 dígitos. Error en fila {row}");
            }
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

                    ValidateTicketNumber(ticket.Number, ticket.Loteria, row);
                    tickets.Add(ticket);
                }
            }
            return tickets;
        }

        public List<Sorteo> ReadExcel2(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var sorteos = new List<Sorteo>();
            FileInfo fileInfo = new FileInfo(filePath);

            using (var package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets["Hoja2"];
                int rowCount = worksheet.Dimension.Rows;

                for(int row = 2; row <= rowCount; row++)
                {
                    var sorteo = new Sorteo
                    {
                        Number = worksheet.Cells[row, 2].Text.Trim(),
                        Serie = FormatText(worksheet.Cells[row, 3].Text.Trim()),
                        Loteria = FormatText(worksheet.Cells[row, 4].Text.Trim()),
                        Date = DateTime.Parse(worksheet.Cells[row, 5].Text.Trim())
                    };
                    sorteos.Add(sorteo);
                }
            }
           return sorteos;
        }
    }
}
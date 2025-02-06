using LoteriaProject.Model;
using OfficeOpenXml;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LoteriaProject.Custom
{
    public class ReadExcel
    {
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
                    string loteriaText = worksheet.Cells[row, 3].Text.Trim();
                    string formattedLoteria = string.Empty;

                    if (!string.IsNullOrEmpty(loteriaText))
                    {
                        // Dividir el texto en palabras y aplicar el formato a cada una
                        formattedLoteria = string.Join(" ",
                            loteriaText.Split(' ')
                            .Select(word => word.Length > 0
                                ? char.ToUpper(word[0]) + word.Substring(1).ToLower()
                                : string.Empty)
                            .Where(word => !string.IsNullOrEmpty(word)));
                    }


                    var ticket = new Ticket
                    {
                        Number = worksheet.Cells[row, 2].Text.Trim(),
                        Loteria = formattedLoteria,
                        sign = worksheet.Cells[row, 4].Text.Trim(),
                        Jornada = worksheet.Cells[row, 5].Text.Trim(),
                        Date = DateTime.Parse(worksheet.Cells[row, 6].Text.Trim()),
                        
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

using Task_RS.DTOs;
using Task_RS.Interfaces;
using ClosedXML.Excel;
using System.Globalization;

namespace Task_RS.Services
{
    public class ExcelMappingService: IExcelMappingService
    {
        public IReadOnlyList<ProductDto> MapProducts(Stream excelStream)
        {
            using var workbook = new XLWorkbook(excelStream);
            var sheet = workbook.Worksheets.First();
            ValidateHeader(sheet);

            var result = new List<ProductDto>();

            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var dto = new ProductDto
                {
                    Name = row.Cell(1).GetString().Trim(),
                    Unit = row.Cell(2).GetString().Trim(),
                    PriceEur = GetDecimal(row.Cell(3)),
                    Quantity = row.Cell(4).GetValue<int>()
                };

                ValidateRow(dto, row.RowNumber());
                result.Add(dto);
            }

            return result;

        }

        private static void ValidateHeader(IXLWorksheet sheet)
        {
            string[] expected =
            {
                "Наименование",
                "Единица измерения",
                "Цена за единицу, евро",
                "Количество, шт."
            };

            for (int i = 0; i < expected.Length; i++)
            {
                var actual = sheet.Cell(1, i + 1).GetString().Trim();

                if (!string.Equals(actual, expected[i], StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Неверный заголовок в колонке {i + 1}. Ожидалось '{expected[i]}', получено '{actual}'");
                }
            }
        }
        private static decimal GetDecimal(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number)
                return cell.GetValue<decimal>();

            if (decimal.TryParse(
                cell.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var value))
            {
                return value;
            }

            throw new InvalidDataException(
                $"Неверное числовое значение: '{cell.GetString()}'");
        }

        private static void ValidateRow(ProductDto dto, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidDataException($"Пустое наименование (строка {rowNumber})");

            if (dto.PriceEur < 0)
                throw new InvalidDataException($"Отрицательная цена (строка {rowNumber})");

            if (dto.Quantity <= 0)
                throw new InvalidDataException($"Количество <= 0 (строка {rowNumber})");
        }
    }
}

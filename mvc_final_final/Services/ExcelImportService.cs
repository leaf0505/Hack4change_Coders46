using OfficeOpenXml;
using mvc_final_final.Models;

namespace mvc_final_final.Services;

public class ExcelImportResult
{
    public List<NeedPreview> Rows { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool HasErrors => Errors.Any();
}

public class NeedPreview
{
    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantityNeeded { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
    public string? RowError { get; set; }
    public bool IsValid => string.IsNullOrEmpty(RowError);
}

public class ExcelImportService
{
    private static readonly string[] ValidCategories =
        { "food", "clothing", "hygiene", "bedding", "beds", "other" };

    private static readonly Dictionary<string, Priority> PriorityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "critical", Priority.Critical }, { "0", Priority.Critical },
        { "normal",   Priority.Normal   }, { "1", Priority.Normal   },
        { "low",      Priority.Low      }, { "2", Priority.Low      }
    };

    public ExcelImportResult Parse(Stream fileStream)
    {
        // EPPlus 7 requires license context for non-commercial use
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var result = new ExcelImportResult();

        using var package = new ExcelPackage(fileStream);
        var sheet = package.Workbook.Worksheets.FirstOrDefault();

        if (sheet == null)
        {
            result.Errors.Add("The Excel file contains no worksheets.");
            return result;
        }

        if (sheet.Dimension == null || sheet.Dimension.Rows < 2)
        {
            result.Errors.Add("The sheet is empty or contains only a header row.");
            return result;
        }

        // Detect header columns (case-insensitive)
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int col = 1; col <= sheet.Dimension.Columns; col++)
        {
            var h = sheet.Cells[1, col].Text?.Trim();
            if (!string.IsNullOrEmpty(h))
                headers[h] = col;
        }

        // Required columns
        var requiredCols = new[] { "ItemName", "QuantityNeeded" };
        foreach (var req in requiredCols)
        {
            if (!headers.ContainsKey(req))
            {
                result.Errors.Add($"Missing required column: \"{req}\". " +
                    $"Expected columns: ItemName, Category, QuantityNeeded, Priority");
                return result;
            }
        }

        // Parse rows
        for (int row = 2; row <= sheet.Dimension.Rows; row++)
        {
            // Skip fully empty rows
            var rowText = string.Concat(Enumerable.Range(1, sheet.Dimension.Columns)
                .Select(c => sheet.Cells[row, c].Text));
            if (string.IsNullOrWhiteSpace(rowText)) continue;

            var preview = new NeedPreview();

            // ItemName
            preview.ItemName = headers.TryGetValue("ItemName", out var iCol)
                ? sheet.Cells[row, iCol].Text?.Trim() ?? ""
                : "";

            if (string.IsNullOrWhiteSpace(preview.ItemName))
            {
                preview.RowError = $"Row {row}: ItemName is empty.";
                result.Rows.Add(preview);
                continue;
            }

            // Category (optional, default = "other")
            if (headers.TryGetValue("Category", out var cCol))
            {
                var cat = sheet.Cells[row, cCol].Text?.Trim().ToLower() ?? "";
                preview.Category = ValidCategories.Contains(cat) ? cat : "other";
            }
            else
            {
                preview.Category = "other";
            }

            // QuantityNeeded
            if (headers.TryGetValue("QuantityNeeded", out var qCol))
            {
                var qText = sheet.Cells[row, qCol].Text?.Trim();
                if (!int.TryParse(qText, out int qty) || qty <= 0)
                {
                    preview.RowError = $"Row {row}: QuantityNeeded must be a positive integer (got \"{qText}\").";
                    result.Rows.Add(preview);
                    continue;
                }
                preview.QuantityNeeded = qty;
            }

            // Priority (optional, default = Normal)
            if (headers.TryGetValue("Priority", out var pCol))
            {
                var pText = sheet.Cells[row, pCol].Text?.Trim() ?? "";
                preview.Priority = PriorityMap.TryGetValue(pText, out var p) ? p : Priority.Normal;
            }

            result.Rows.Add(preview);
        }

        if (!result.Rows.Any())
            result.Errors.Add("No data rows were found in the file.");

        return result;
    }
}

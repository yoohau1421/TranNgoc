
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Text.Json;
using TranNgoc.Data;
using TranNgoc.Services.Dto;
using TranNgoc_BE.Models;
using TranNgoc_BE.Services.Dto.ExcelCompare;
using TranNgoc_BE.Services.Interfaces;

namespace TranNgoc_BE.Services
{
    public class CompareExcelService : ICompareExcelService
    {
        private readonly AppDbContext _dbContext;

        public CompareExcelService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<BaseResponse<CompareExcelResultDto>> DownloadSampleFile(long objectId)
        {
            var response = new BaseResponse<CompareExcelResultDto>();

            try
            {
                var template = await GetTemplateAsync(objectId);

                using var package = new ExcelPackage();
                var sheet = package.Workbook.Worksheets.Add("File mẫu");

                var columns = template.Columns
                    .OrderBy(x => x.ExcelIndex)
                    .ToList();

                foreach (var col in columns)
                {
                    sheet.Cells[1, col.ExcelIndex].Value = col.ColumnName;
                    sheet.Cells[1, col.ExcelIndex].Style.Font.Bold = true;
                }

                sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

                response.IsSuccess = true;
                response.Message = "Tải file mẫu thành công";
                response.Data = new CompareExcelResultDto
                {
                    FileBytes = package.GetAsByteArray(),
                    FileName = $"file-mau-{template.Code}-{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }
        public async Task<BaseResponse<List<CompareTemplateOptionDto>>> GetTemplateOptionsAsync()
        {
            var response = new BaseResponse<List<CompareTemplateOptionDto>>();

            try
            {
                var data = await _dbContext.CompareTemplates
                    .Where(x => x.IsActive)
                    .GroupBy(x => x.ObjectId)
                    .Select(g => new CompareTemplateOptionDto
                    {
                        ObjectId = g.Key,
                        Name = g.First().Name
                    })
                    .OrderBy(x => x.ObjectId)
                    .ToListAsync();

                response.IsSuccess = true;
                response.Data = data;
                response.Message = "Lấy danh sách template thành công";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }
        public async Task<BaseResponse<DynamicComparePreviewResultDto>> CompareReview(IFormFile file, long objectId)
        {
            var response = new BaseResponse<DynamicComparePreviewResultDto>();

            try
            {
                ValidateFile(file);

                var template = await GetTemplateAsync(objectId);

                using var package = new ExcelPackage(file.OpenReadStream());
                var sheet = package.Workbook.Worksheets.FirstOrDefault();

                if (sheet == null)
                    throw new Exception("File Excel không có sheet dữ liệu.");

                var rows = ReadExcelByTemplate(sheet, template);

                await ProcessCompareByTemplate(rows, template);

                response.IsSuccess = true;
                response.Message = "Đối soát dữ liệu thành công";
                response.Data = new DynamicComparePreviewResultDto
                {
                    TotalRows = rows.Count,
                    SuccessRows = rows.Count(x => x.IsValid),
                    ErrorRows = rows.Count(x => !x.IsValid),
                    DisplayColumns = BuildDisplayColumns(template),
                    Rows = rows
                };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<BaseResponse<CompareExcelResultDto>> ExportExcel(IFormFile file, long objectId)
        {
            var response = new BaseResponse<CompareExcelResultDto>();

            try
            {
                ValidateFile(file);

                var template = await GetTemplateAsync(objectId);

                using var package = new ExcelPackage(file.OpenReadStream());
                var sheet = package.Workbook.Worksheets.FirstOrDefault();

                if (sheet == null)
                    throw new Exception("File Excel không có sheet dữ liệu.");

                var rows = ReadExcelByTemplate(sheet, template);

                await ProcessCompareByTemplate(rows, template);

                WriteResultToExcel(sheet, rows, template);

                response.IsSuccess = true;
                response.Message = "Xuất file thành công";
                response.Data = new CompareExcelResultDto
                {
                    FileBytes = package.GetAsByteArray(),
                    FileName = $"ket-qua-doi-soat-{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }

            return response;
        }

        private async Task<CompareTemplate> GetTemplateAsync(long objectId)
        {
            var template = await _dbContext.CompareTemplates
                .Include(x => x.Columns)
                .Include(x => x.RuleConfigs)
                .FirstOrDefaultAsync(x => x.ObjectId == objectId && x.IsActive);

            if (template == null)
                throw new Exception("Chưa cấu hình template đối soát cho objectId này.");

            if (!template.Columns.Any())
                throw new Exception("Template chưa cấu hình cột Excel.");

            return template;
        }

        private List<DynamicCompareRowDto> ReadExcelByTemplate(ExcelWorksheet worksheet, CompareTemplate template)
        {
            var rows = new List<DynamicCompareRowDto>();
            var lastRow = worksheet.Dimension?.End.Row ?? 0;

            var columns = template.Columns
                .OrderBy(x => x.ExcelIndex)
                .ToList();

            for (int rowIndex = 2; rowIndex <= lastRow; rowIndex++)
            {
                var row = new DynamicCompareRowDto
                {
                    RowIndex = rowIndex
                };

                var hasValue = false;

                foreach (var col in columns)
                {
                    var text = worksheet.Cells[rowIndex, col.ExcelIndex].Text?.Trim();

                    row.RawValues[col.ColumnKey] = text;

                    if (!string.IsNullOrWhiteSpace(text))
                        hasValue = true;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        row.Values[col.ColumnKey] = null;

                        if (col.IsRequired)
                            row.Errors.Add($"{col.ColumnName} không được để trống");

                        continue;
                    }

                    if (col.DataType.Equals("number", StringComparison.OrdinalIgnoreCase))
                    {
                        var number = TryGetDecimal(text);

                        row.Values[col.ColumnKey] = number;

                        if (number == null)
                            row.Errors.Add($"{col.ColumnName} không hợp lệ");
                    }
                    else
                    {
                        row.Values[col.ColumnKey] = text;
                    }
                }

                if (hasValue)
                    rows.Add(row);
            }

            return rows;
        }

        private async Task ProcessCompareByTemplate(List<DynamicCompareRowDto> rows, CompareTemplate template)
        {
            switch (template.Code)
            {
                case "DISTANCE_WEIGHT":
                    await CompareDistanceWeightAsync(rows, template);
                    break;

                case "ROUTE_FIXED":
                    await CompareRouteFixedAsync(rows, template);
                    break;

                default:
                    throw new Exception($"Template chưa được hỗ trợ: {template.Code}");
            }
        }

        private async Task CompareDistanceWeightAsync(List<DynamicCompareRowDto> rows, CompareTemplate template)
        {
            var masterData = await _dbContext.CompareMasterData
                .Where(x => x.TemplateId == template.Id && x.IsActive)
                .ToListAsync();

            var config = template.RuleConfigs
                .Where(x => x.IsActive)
                .ToDictionary(x => x.ConfigKey, x => x.ConfigValue);

            var loadingUnitPrice = GetConfigDecimal(config, "loading_unit_price", 100000);
            var overnightUnitPrice = GetConfigDecimal(config, "overnight_unit_price", 800000);

            foreach (var row in rows)
            {
                if (row.Errors.Any())
                    continue;

                var soKm = GetDecimal(row, "so_km");
                var trongTai = GetDecimal(row, "trong_tai");
                var donGiaImport = GetDecimal(row, "don_gia");

                if (soKm == null || trongTai == null || donGiaImport == null)
                    continue;

                var master = masterData.FirstOrDefault(x =>
                {
                    using var doc = JsonDocument.Parse(x.DataJson);
                    var json = doc.RootElement;

                    var distanceFrom = GetJsonDecimal(json, "distanceFromKm");
                    var distanceTo = GetJsonDecimal(json, "distanceToKm");
                    var tonFrom = GetJsonDecimal(json, "tonFrom");
                    var tonTo = GetJsonDecimal(json, "tonTo");

                    return
                        (distanceFrom == null || distanceFrom <= soKm) &&
                        (distanceTo == null || distanceTo >= soKm) &&
                        (tonFrom == null || tonFrom < trongTai) &&
                        (tonTo == null || tonTo >= trongTai);
                });

                if (master == null)
                {
                    row.Errors.Add("Không tìm thấy dữ liệu chuẩn");
                    continue;
                }

                row.StandardPrice = master.Price;
                if (master.Price == null)
                {
                    row.Errors.Add("Dữ liệu chuẩn chưa cấu hình giá");
                    continue;
                }
                if (donGiaImport != master.Price)
                    row.Errors.Add($"Sai đơn giá. Giá chuẩn: {master.Price:N0}");

                var trongLuongBocXep = GetDecimal(row, "trong_luong_boc_xep");
                var phiBocXepImport = GetDecimal(row, "phi_boc_xep");

                if (trongLuongBocXep != null && phiBocXepImport != null)
                {
                    row.StandardLoadingFee = trongLuongBocXep.Value * loadingUnitPrice;

                    if (phiBocXepImport != row.StandardLoadingFee)
                        row.Errors.Add($"Sai phí bốc xếp. Phí chuẩn: {row.StandardLoadingFee:N0}");
                }

                var quaDem = GetDecimal(row, "qua_dem");

                if (quaDem != null)
                {
                    row.StandardOvernightFee = quaDem.Value * overnightUnitPrice;
                }
            }
        }

        private async Task CompareRouteFixedAsync(List<DynamicCompareRowDto> rows, CompareTemplate template)
        {
            var masterData = await _dbContext.CompareMasterData
                .Where(x => x.TemplateId == template.Id && x.IsActive)
                .ToListAsync();

            foreach (var row in rows)
            {
                if (row.Errors.Any())
                    continue;

                var origin = GetText(row, "origin");
                var destination = GetText(row, "destination");
                var soKm = GetDecimal(row, "so_km");
                var donGiaImport = GetDecimal(row, "don_gia");

                if (string.IsNullOrWhiteSpace(origin) ||
                    string.IsNullOrWhiteSpace(destination) ||
                    donGiaImport == null)
                {
                    continue;
                }

                var master = masterData.FirstOrDefault(x =>
                {
                    using var doc = JsonDocument.Parse(x.DataJson);
                    var json = doc.RootElement;

                    var masterOrigin = GetJsonText(json, "origin");
                    var masterDestination = GetJsonText(json, "destination");

                    return NormalizeText(masterOrigin) == NormalizeText(origin)
                        && NormalizeText(masterDestination) == NormalizeText(destination);
                });

                if (master == null)
                {
                    row.Errors.Add("Không tìm thấy tuyến chuẩn");
                    continue;
                }

                row.StandardPrice = master.Price;

                if (donGiaImport != master.Price)
                    row.Errors.Add($"Sai đơn giá. Giá chuẩn: {master.Price:N0}");

                using var masterDoc = JsonDocument.Parse(master.DataJson);
                var masterDistance = GetJsonDecimal(masterDoc.RootElement, "distanceKm");

                if (soKm != null && masterDistance != null && soKm != masterDistance)
                    row.Errors.Add($"Sai số KM. KM chuẩn: {masterDistance}");
            }
        }

        private void WriteResultToExcel(ExcelWorksheet sheet, List<DynamicCompareRowDto> rows, CompareTemplate template)
        {
            var lastInputColumn = template.Columns.Max(x => x.ExcelIndex);

            var hasLoadingFee = template.Columns.Any(x => x.ColumnKey == "phi_boc_xep");
            var hasOvernight = template.Columns.Any(x => x.ColumnKey == "qua_dem");

            var colIndex = lastInputColumn + 1;

            var resultCol = colIndex++;
            var standardPriceCol = colIndex++;

            int? standardLoadingFeeCol = null;
            int? standardOvernightFeeCol = null;

            if (hasLoadingFee)
                standardLoadingFeeCol = colIndex++;

            if (hasOvernight)
                standardOvernightFeeCol = colIndex++;

            var errorCol = colIndex;

            sheet.Cells[1, resultCol].Value = "Kết quả";
            sheet.Cells[1, standardPriceCol].Value = "Giá chuẩn";

            if (standardLoadingFeeCol.HasValue)
                sheet.Cells[1, standardLoadingFeeCol.Value].Value = "Phí bốc xếp chuẩn";

            if (standardOvernightFeeCol.HasValue)
                sheet.Cells[1, standardOvernightFeeCol.Value].Value = "Phí qua đêm chuẩn";

            sheet.Cells[1, errorCol].Value = "Thông tin lỗi";

            foreach (var row in rows)
            {
                sheet.Cells[row.RowIndex, resultCol].Value = row.IsValid ? "Đúng" : "Sai";
                sheet.Cells[row.RowIndex, standardPriceCol].Value = row.StandardPrice;

                if (standardLoadingFeeCol.HasValue)
                    sheet.Cells[row.RowIndex, standardLoadingFeeCol.Value].Value = row.StandardLoadingFee;

                if (standardOvernightFeeCol.HasValue)
                    sheet.Cells[row.RowIndex, standardOvernightFeeCol.Value].Value = row.StandardOvernightFee;

                sheet.Cells[row.RowIndex, errorCol].Value = string.Join("; ", row.Errors);
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private decimal? GetDecimal(DynamicCompareRowDto row, string key)
        {
            if (!row.Values.TryGetValue(key, out var value))
                return null;

            return value as decimal?;
        }

        private string? GetText(DynamicCompareRowDto row, string key)
        {
            if (!row.Values.TryGetValue(key, out var value))
                return null;

            return value?.ToString();
        }

        private decimal GetConfigDecimal(Dictionary<string, string> config, string key, decimal defaultValue)
        {
            if (!config.TryGetValue(key, out var value))
                return defaultValue;

            return TryGetDecimal(value) ?? defaultValue;
        }

        private decimal? GetJsonDecimal(JsonElement json, string key)
        {
            if (!json.TryGetProperty(key, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var value))
                return value;

            if (prop.ValueKind == JsonValueKind.String)
                return TryGetDecimal(prop.GetString());

            return null;
        }

        private string? GetJsonText(JsonElement json, string key)
        {
            if (!json.TryGetProperty(key, out var prop))
                return null;

            return prop.ValueKind == JsonValueKind.Null ? null : prop.ToString();
        }

        private string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(" ", value
                .Trim()
                .ToUpperInvariant()
                .Replace(",", "")
                .Replace(".", "")
                .Replace("-", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private decimal? TryGetDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value
                .Trim()
                .Replace("VND", "", StringComparison.OrdinalIgnoreCase)
                .Replace("VNĐ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("đ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("km", "", StringComparison.OrdinalIgnoreCase)
                .Replace("ton", "", StringComparison.OrdinalIgnoreCase)
                .Replace("tấn", "", StringComparison.OrdinalIgnoreCase)
                .Replace(",", "")
                .Replace(" ", "");

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;

            return null;
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File Excel không hợp lệ.");

            var extension = Path.GetExtension(file.FileName).ToLower();

            if (extension != ".xlsx")
                throw new Exception("Chỉ hỗ trợ file .xlsx.");
        }

        private List<CompareDisplayColumnDto> BuildDisplayColumns(CompareTemplate template)
        {
            var columns = template.Columns
                .OrderBy(x => x.ExcelIndex)
                .Select(x => new CompareDisplayColumnDto
                {
                    ColumnKey = x.ColumnKey,
                    ColumnName = x.ColumnName,
                    Group = "IMPORT",
                    DataType = x.DataType,
                    Align = x.DataType == "number" ? "right" : "left"
                })
                .ToList();

            columns.Add(new CompareDisplayColumnDto
            {
                ColumnKey = "standardPrice",
                ColumnName = "Đơn giá chuẩn",
                Group = "STANDARD",
                DataType = "number",
                Align = "right"
            });

            if (template.Columns.Any(x => x.ColumnKey == "phi_boc_xep"))
            {
                columns.Add(new CompareDisplayColumnDto
                {
                    ColumnKey = "standardLoadingFee",
                    ColumnName = "Phí BX chuẩn",
                    Group = "STANDARD",
                    DataType = "number",
                    Align = "right"
                });
            }

            if (template.Columns.Any(x => x.ColumnKey == "qua_dem"))
            {
                columns.Add(new CompareDisplayColumnDto
                {
                    ColumnKey = "standardOvernightFee",
                    ColumnName = "Phí qua đêm chuẩn",
                    Group = "STANDARD",
                    DataType = "number",
                    Align = "right"
                });
            }

            columns.Add(new CompareDisplayColumnDto
            {
                ColumnKey = "result",
                ColumnName = "Kết quả",
                Group = "RESULT",
                DataType = "text",
                Align = "center"
            });

            columns.Add(new CompareDisplayColumnDto
            {
                ColumnKey = "errors",
                ColumnName = "Lỗi",
                Group = "RESULT",
                DataType = "text",
                Align = "left"
            });

            return columns;
        }
    }
}

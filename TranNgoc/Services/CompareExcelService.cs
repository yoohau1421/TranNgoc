using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using TranNgoc.Data;
using TranNgoc.Models;
using TranNgoc.Services.Dto;
using TranNgoc.Services.Interfaces;

namespace TranNgoc.Services
{
    public class CompareExcelService : ICompareExcelService
    {
        private readonly AppDbContext _dbContext;

        public CompareExcelService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CompareExcelResultDto> CompareAsync(IFormFile file, long objectId)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File Excel không hợp lệ.");

            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                throw new Exception("Không tìm thấy sheet dữ liệu.");

            var lastRow = worksheet.Dimension?.End.Row ?? 0;

            const int resultColumn = 8;
            const int masterPriceColumn = 9;
            const int loadingFeeStandardColumn = 10;
            const int detentionFeeStandardColumn = 11;
            const int errorColumn = 12;

            worksheet.Cells[1, resultColumn].Value = "Kết quả";
            worksheet.Cells[1, masterPriceColumn].Value = "Đơn giá chuẩn";
            worksheet.Cells[1, loadingFeeStandardColumn].Value = "Phí bốc xếp chuẩn";
            worksheet.Cells[1, detentionFeeStandardColumn].Value = "Phí qua đêm chuẩn";
            worksheet.Cells[1, errorColumn].Value = "Thông tin lỗi";

            int totalRows = 0;
            int successRows = 0;
            int errorRows = 0;

            for (int rowIndex = 2; rowIndex <= lastRow; rowIndex++)
            {
                if (IsEmptyRow(worksheet, rowIndex))
                    continue;

                totalRows++;

                var soKm = TryGetDecimal(worksheet.Cells[rowIndex, 2].Text);
                var trongTai = TryGetDecimal(worksheet.Cells[rowIndex, 3].Text);
                var donGiaImport = TryGetDecimal(worksheet.Cells[rowIndex, 4].Text);
                var trongLuongBocXep = TryGetDecimal(worksheet.Cells[rowIndex, 5].Text);
                var phiBocXepImport = TryGetDecimal(worksheet.Cells[rowIndex, 6].Text);
                var quaDem = TryGetDecimal(worksheet.Cells[rowIndex, 7].Text);

                var errors = new List<string>();

                if (soKm == null)
                {
                    errors.Add("Số KM không hợp lệ");
                    MarkErrorCell(worksheet.Cells[rowIndex, 2]);
                }

                if (trongTai == null)
                {
                    errors.Add("Trọng tải tính phí không hợp lệ");
                    MarkErrorCell(worksheet.Cells[rowIndex, 3]);
                }

                if (donGiaImport == null)
                {
                    errors.Add("Đơn giá không hợp lệ");
                    MarkErrorCell(worksheet.Cells[rowIndex, 4]);
                }

                MasterData? masterData = null;

                if (soKm != null && trongTai != null && donGiaImport != null)
                {
                    masterData = await FindMasterDataAsync(objectId, soKm.Value, trongTai.Value);

                    if (masterData == null)
                    {
                        errors.Add("Không tìm thấy dữ liệu chuẩn phù hợp theo KM, trọng tải và đơn vị tính");
                    }
                    else
                    {
                        worksheet.Cells[rowIndex, masterPriceColumn].Value = masterData.Price;

                        if (donGiaImport.Value != masterData.Price)
                        {
                            errors.Add($"Đơn giá sai. File: {donGiaImport.Value:N0}, Chuẩn: {masterData.Price:N0}");
                            MarkErrorCell(worksheet.Cells[rowIndex, 4]);
                        }
                    }
                }

                // Check phí bốc xếp: Loading/unloading fee = 100,000 / Ton
                if (trongLuongBocXep != null || phiBocXepImport != null)
                {
                    if (trongLuongBocXep == null)
                    {
                        errors.Add("Trọng lượng bốc xếp không hợp lệ");
                        MarkErrorCell(worksheet.Cells[rowIndex, 5]);
                    }

                    if (phiBocXepImport == null)
                    {
                        errors.Add("Phí bốc xếp không hợp lệ");
                        MarkErrorCell(worksheet.Cells[rowIndex, 6]);
                    }

                    if (trongLuongBocXep != null && phiBocXepImport != null)
                    {
                        var phiBocXepChuan = trongLuongBocXep.Value * 100000;
                        worksheet.Cells[rowIndex, loadingFeeStandardColumn].Value = phiBocXepChuan;

                        if (phiBocXepImport.Value != phiBocXepChuan)
                        {
                            errors.Add($"Phí bốc xếp sai. File: {phiBocXepImport.Value:N0}, Chuẩn: {phiBocXepChuan:N0}");
                            MarkErrorCell(worksheet.Cells[rowIndex, 6]);
                        }
                    }
                }

                // Check qua đêm: Truck detention = 800,000 / night
                if (quaDem != null)
                {
                    var phiQuaDemChuan = quaDem.Value * 800000;
                    worksheet.Cells[rowIndex, detentionFeeStandardColumn].Value = phiQuaDemChuan;
                }
                else
                {
                    errors.Add("Qua đêm không hợp lệ");
                    MarkErrorCell(worksheet.Cells[rowIndex, 7]);
                }

                if (errors.Any())
                {
                    errorRows++;

                    worksheet.Cells[rowIndex, resultColumn].Value = "Sai";
                    worksheet.Cells[rowIndex, errorColumn].Value = string.Join("; ", errors);

                    MarkErrorCell(worksheet.Cells[rowIndex, resultColumn]);
                    MarkErrorCell(worksheet.Cells[rowIndex, errorColumn]);
                }
                else
                {
                    successRows++;

                    worksheet.Cells[rowIndex, resultColumn].Value = "Đúng";
                    worksheet.Cells[rowIndex, errorColumn].Value = "";

                    MarkSuccessCell(worksheet.Cells[rowIndex, resultColumn]);
                }
            }

            worksheet.Cells.AutoFitColumns();

            var fileBytes = package.GetAsByteArray();

            return new CompareExcelResultDto
            {
                IsSuccess = true,
                Message = $"Đối chiếu hoàn tất. Tổng: {totalRows}, Đúng: {successRows}, Sai: {errorRows}.",
                TotalRows = totalRows,
                SuccessRows = successRows,
                ErrorRows = errorRows,
                FileBytes = fileBytes,
                FileName = $"ket-qua-doi-chieu-{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };
        }

        private async Task<MasterData?> FindMasterDataAsync(long objectId, decimal soKm, decimal trongTai)
        {
            var expectedUnit = soKm < 12 ? "PER_TRIP" : "PER_KM";

            return await _dbContext.MasterData
                .Where(x =>
                    x.ObjectId == objectId &&
                    x.IsActive &&
                    x.Unit == expectedUnit &&
                    (x.DistanceFromKm == null || soKm >= x.DistanceFromKm) &&
                    (x.DistanceToKm == null || soKm <= x.DistanceToKm) &&
                    (x.TonFrom == null || trongTai > x.TonFrom) &&
                    (x.TonTo == null || trongTai <= x.TonTo)
                )
                .OrderBy(x => x.DistanceFromKm ?? 0)
                .ThenBy(x => x.TonFrom ?? 0)
                .FirstOrDefaultAsync();
        }

        private bool IsEmptyRow(ExcelWorksheet worksheet, int rowIndex)
        {
            for (int col = 1; col <= 7; col++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cells[rowIndex, col].Text))
                    return false;
            }

            return true;
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

            if (decimal.TryParse(value, out var result))
                return result;

            return null;
        }

        private void MarkErrorCell(ExcelRange cell)
        {
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.LightPink);
            cell.Style.Font.Color.SetColor(Color.DarkRed);
            cell.Style.Font.Bold = true;
        }

        private void MarkSuccessCell(ExcelRange cell)
        {
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
            cell.Style.Font.Color.SetColor(Color.DarkGreen);
            cell.Style.Font.Bold = true;
        }

        public async Task<List<ImportCompareExcelRowDto>> ImportExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File Excel không hợp lệ.");

            var extension = Path.GetExtension(file.FileName).ToLower();

            if (extension != ".xlsx")
                throw new Exception("EPPlus khuyến nghị dùng file .xlsx.");

            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                throw new Exception("File Excel không có sheet dữ liệu.");

            var rows = new List<ImportCompareExcelRowDto>();

            var firstDataRow = 2;
            var lastRow = worksheet.Dimension?.End.Row ?? 0;

            for (int rowIndex = firstDataRow; rowIndex <= lastRow; rowIndex++)
            {
                if (IsEmptyRow(worksheet, rowIndex))
                    continue;

                var item = new ImportCompareExcelRowDto
                {
                    RowIndex = rowIndex,
                    Stt = TryGetInt(worksheet.Cells[rowIndex, 1].Text),
                    SoKm = TryGetDecimal(worksheet.Cells[rowIndex, 2].Text),
                    TrongTaiTinhPhi = TryGetDecimal(worksheet.Cells[rowIndex, 3].Text),
                    DonGia = TryGetDecimal(worksheet.Cells[rowIndex, 4].Text),
                    TrongLuongBocXep = TryGetDecimal(worksheet.Cells[rowIndex, 5].Text),
                    PhiBocXep = TryGetDecimal(worksheet.Cells[rowIndex, 6].Text),
                    QuaDem = TryGetDecimal(worksheet.Cells[rowIndex, 7].Text)
                };

                ValidateRow(item);
                rows.Add(item);
            }

            return rows;
        }

        private void ValidateRow(ImportCompareExcelRowDto row)
        {
            if (row.SoKm == null)
                row.Errors.Add("Số KM không hợp lệ.");

            if (row.TrongTaiTinhPhi == null)
                row.Errors.Add("Trọng tải tính phí không hợp lệ.");

            if (row.DonGia == null)
                row.Errors.Add("Đơn giá không hợp lệ.");

            row.IsValid = row.Errors.Count == 0;
        }

        private int? TryGetInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = NormalizeNumber(value);

            if (int.TryParse(value, out var result))
                return result;

            return null;
        }

        private string NormalizeNumber(string value)
        {
            return value
                .Trim()
                .Replace("VND", "", StringComparison.OrdinalIgnoreCase)
                .Replace("VNĐ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("đ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("km", "", StringComparison.OrdinalIgnoreCase)
                .Replace("ton", "", StringComparison.OrdinalIgnoreCase)
                .Replace("tấn", "", StringComparison.OrdinalIgnoreCase)
                .Replace(",", "")
                .Replace(" ", "");
        }
    }
}

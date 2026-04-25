using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using TranNgoc.Data;
using TranNgoc.Models;
using TranNgoc.Services.Interfaces;
using UglyToad.PdfPig;

namespace TranNgoc.Services
{
    public class MasterDataPdfImportService : IMasterDataPdfImportService
    {
        private readonly AppDbContext _dbContext;
        public MasterDataPdfImportService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<int> ImportFromPdfAsync(IFormFile file, long objectId)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File PDF không hợp lệ.");

            var text = await ExtractTextFromPdfAsync(file);

            Console.WriteLine("===== PDF TEXT START =====");
            Console.WriteLine(text);
            Console.WriteLine("===== PDF TEXT END =====");


            var masterDataList = ParseTransportRates(text, objectId);

            if (!masterDataList.Any())
                throw new Exception("Không đọc được dữ liệu bảng giá từ PDF.");

            _dbContext.Set<MasterData>().AddRange(masterDataList);
            await _dbContext.SaveChangesAsync();

            return masterDataList.Count;
        }
        private async Task<string> ExtractTextFromPdfAsync(IFormFile file)
        {
            var tempFilePath = Path.GetTempFileName();

            try
            {
                await using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                using var document = PdfDocument.Open(tempFilePath);

                var lines = new List<string>();

                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text;

                    lines.Add(pageText);
                }

                return string.Join(Environment.NewLine, lines);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
        private List<MasterData> ParseTransportRates(string text, long objectId)
        {
            var result = new List<MasterData>();

            var lines = text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            decimal? currentDistanceFrom = null;
            decimal? currentDistanceTo = null;
            string currentUnit = "PER_KM";
            string? currentDescription = null;

            foreach (var line in lines)
            {
                var normalizedLine = NormalizeText(line);

                if (TryParseDistanceRange(normalizedLine, out var distanceFrom, out var distanceTo, out var unit, out var description))
                {
                    currentDistanceFrom = distanceFrom;
                    currentDistanceTo = distanceTo;
                    currentUnit = unit;
                    currentDescription = description;
                    continue;
                }

                if (TryParseRateLine(normalizedLine, out var tonFrom, out var tonTo, out var price))
                {
                    result.Add(new MasterData
                    {
                        ObjectId = objectId,
                        DistanceFromKm = currentDistanceFrom,
                        DistanceToKm = currentDistanceTo,
                        TonFrom = tonFrom,
                        TonTo = tonTo,
                        Unit = currentUnit,
                        Price = price,
                        Currency = "VND",
                        Description = currentDescription,
                        IsActive = true
                    });
                }
            }

            return result;
        }
        private string NormalizeText(string input)
        {
            return input
                .Replace("≤", "<=")
                .Replace("–", "-")
                .Replace("—", "-")
                .Replace(",", "")
                .Replace("VND", " VND ")
                .Replace("Ton", " Ton ")
                .Replace("ton", " Ton ")
                .Trim();
        }

        private bool TryParseDistanceRange(
        string line,
        out decimal? distanceFrom,
        out decimal? distanceTo,
        out string unit,
        out string? description)
        {
            distanceFrom = null;
            distanceTo = null;
            unit = "PER_KM";
            description = null;

            // Less than 12 km
            var lessThanMatch = Regex.Match(line, @"Less than\s+(\d+(\.\d+)?)\s*km", RegexOptions.IgnoreCase);
            if (lessThanMatch.Success)
            {
                distanceFrom = null;
                distanceTo = ParseDecimal(lessThanMatch.Groups[1].Value);
                unit = "PER_TRIP";
                description = lessThanMatch.Value;
                return true;
            }

            // 13 - 40 km
            var rangeMatch = Regex.Match(line, @"(\d+(\.\d+)?)\s*-\s*(\d+(\.\d+)?)\s*km", RegexOptions.IgnoreCase);
            if (rangeMatch.Success)
            {
                distanceFrom = ParseDecimal(rangeMatch.Groups[1].Value);
                distanceTo = ParseDecimal(rangeMatch.Groups[3].Value);
                unit = "PER_KM";
                description = rangeMatch.Value;
                return true;
            }

            return false;
        }

        private bool TryParseRateLine(
        string line,
        out decimal? tonFrom,
        out decimal? tonTo,
        out decimal price)
        {
            tonFrom = null;
            tonTo = null;
            price = 0;

            if (!line.Contains("Ton", StringComparison.OrdinalIgnoreCase))
                return false;

            var priceMatch = Regex.Match(line, @"VND\s*(\d+)", RegexOptions.IgnoreCase);
            if (!priceMatch.Success)
                return false;

            price = ParseDecimal(priceMatch.Groups[1].Value);

            // X <= 1 Ton
            var lessOrEqualMatch = Regex.Match(
                line,
                @"X\s*<=\s*(\d+(\.\d+)?)\s*Ton",
                RegexOptions.IgnoreCase);

            if (lessOrEqualMatch.Success)
            {
                tonFrom = null;
                tonTo = ParseDecimal(lessOrEqualMatch.Groups[1].Value);
                return true;
            }

            // 1 Ton < X <= 1.8 Ton
            var rangeMatch = Regex.Match(
                line,
                @"(\d+(\.\d+)?)\s*Ton\s*<\s*X\s*<=\s*(\d+(\.\d+)?)\s*Ton",
                RegexOptions.IgnoreCase);

            if (rangeMatch.Success)
            {
                tonFrom = ParseDecimal(rangeMatch.Groups[1].Value);
                tonTo = ParseDecimal(rangeMatch.Groups[3].Value);
                return true;
            }

            return false;
        }

        private decimal ParseDecimal(string value)
        {
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}

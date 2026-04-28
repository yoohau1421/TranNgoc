namespace TranNgoc.Services.Dto.ExcelCompare
{
    public class ComparePreviewResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int ErrorRows { get; set; }

        public List<CompareRowResultDto> Rows { get; set; } = new();
    }

    public class CompareRowResultDto
    {
        public int RowIndex { get; set; }

        public decimal? SoKm { get; set; }
        public decimal? TrongTai { get; set; }
        public decimal? DonGiaImport { get; set; }

        public decimal? TrongLuongBocXep { get; set; }
        public decimal? PhiBocXepImport { get; set; }
        public decimal? QuaDem { get; set; }

        public decimal? DonGiaChuan { get; set; }
        public decimal? PhiBocXepChuan { get; set; }
        public decimal? PhiQuaDemChuan { get; set; }

        public bool IsValid { get; set; }
        public string ResultText => IsValid ? "Đúng" : "Sai";

        public List<string> Errors { get; set; } = new();
    }
}

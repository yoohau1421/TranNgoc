namespace TranNgoc.Services.Dto
{
    public class ImportCompareExcelRowDto
    {
        public int RowIndex { get; set; }
        public int? Stt { get; set; }
        public decimal? SoKm { get; set; }
        public decimal? TrongTaiTinhPhi { get; set; }
        public decimal? DonGia { get; set; }
        public decimal? TrongLuongBocXep { get; set; }
        public decimal? PhiBocXep { get; set; }
        public decimal? QuaDem { get; set; }

        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
    }
}

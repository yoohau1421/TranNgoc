namespace TranNgoc_BE.Services.Dto.ExcelCompare
{
    public class DynamicCompareRowDto
    {
        public int RowIndex { get; set; }

        public Dictionary<string, string?> RawValues { get; set; } = new();

        public Dictionary<string, object?> Values { get; set; } = new();

        public decimal? StandardPrice { get; set; }

        public decimal? StandardLoadingFee { get; set; }

        public decimal? StandardOvernightFee { get; set; }

        public List<string> Errors { get; set; } = new();

        public bool IsValid => !Errors.Any();
    }
    public class DynamicComparePreviewResultDto
    {
        public int TotalRows { get; set; }

        public int SuccessRows { get; set; }

        public int ErrorRows { get; set; }

        public List<CompareDisplayColumnDto> DisplayColumns { get; set; } = new();

        public List<DynamicCompareRowDto> Rows { get; set; } = new();
    }

    public class CompareDisplayColumnDto
    {
        public string ColumnKey { get; set; } = string.Empty;

        public string ColumnName { get; set; } = string.Empty;

        public string Group { get; set; } = "IMPORT"; // IMPORT, STANDARD, RESULT

        public string DataType { get; set; } = "text";

        public string Align { get; set; } = "left";
    }
}

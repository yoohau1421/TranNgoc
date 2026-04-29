namespace TranNgoc_BE.Models
{
    public class CompareTemplateColumn
    {
        public long Id { get; set; }
        public long TemplateId { get; set; }
        public string ColumnKey { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public int ExcelIndex { get; set; }
        public string DataType { get; set; } = "text";
        public bool IsRequired { get; set; } = false;
        public CompareTemplate Template { get; set; } = null!;
    }
}

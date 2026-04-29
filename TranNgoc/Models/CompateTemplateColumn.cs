namespace TranNgoc_BE.Models
{
    public class CompateTemplateColumn
    {
        public long Id { get; set; }
        public long TemplateId { get; set; }
        public string ColumnKey { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public int ExcelIndex { get; set; }
        public string DataType { get; set; } = "text";
        public bool IsRequired { get; set; } = false;
    }
}

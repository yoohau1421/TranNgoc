using System.ComponentModel.DataAnnotations;

namespace TranNgoc_BE.Services.Dto.ExcelCompare
{
    public class ImportCompareExcelRequest
    {
        public IFormFile File { get; set; } = null!;
        public long ObjectId { get; set; }
    }
}

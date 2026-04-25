using System.ComponentModel.DataAnnotations;

namespace TranNgoc.Services.Dto
{
    public class ImportCompareExcelRequest
    {
        public IFormFile File { get; set; } = null!;
        public long ObjectId { get; set; }
    }
}

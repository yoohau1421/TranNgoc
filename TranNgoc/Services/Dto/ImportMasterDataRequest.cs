using System.ComponentModel.DataAnnotations;

namespace TranNgoc.Services.Dto
{
    public class ImportMasterDataRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public long ObjectId { get; set; }
    }
}

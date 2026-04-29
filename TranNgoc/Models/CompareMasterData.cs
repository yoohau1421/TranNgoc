using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TranNgoc_BE.Models
{
    public class CompareMasterData
    {
        public long Id { get; set; }
        public long TemplateId { get; set; }

        public string DataJson { get; set; } 

        public decimal? Price { get; set; }

        public string? Unit { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public CompareTemplate Template { get; set; } = null!;
    }
}

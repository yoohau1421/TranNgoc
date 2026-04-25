using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TranNgoc.Models
{
    public class MasterData
    {
        public long Id { get; set; }

        // loại đối chiếu (vd: báo giá leschaco)
        public long ObjectId { get; set; }

        // khoảng KM
        public decimal? DistanceFromKm { get; set; }
        public decimal? DistanceToKm { get; set; }

        // khoảng trọng tải
        public decimal? TonFrom { get; set; }
        public decimal? TonTo { get; set; }

        // đơn vị
        public string Unit { get; set; } // PER_KM | PER_TRIP

        // giá
        public decimal Price { get; set; }

        // tiền tệ
        public string Currency { get; set; }

        // mô tả (optional)
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

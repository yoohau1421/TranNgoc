namespace TranNgoc_BE.Models
{
    public class CompareTemplate
    {
        public long Id { get; set; }
        public long ObjectId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool isActive { get; set; }
    }
}

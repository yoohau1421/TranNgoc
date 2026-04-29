namespace TranNgoc_BE.Models
{
    public class CompareTemplate
    {
        public long Id { get; set; }
        public long ObjectId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<CompareTemplateColumn> Columns { get; set; } = new List<CompareTemplateColumn>();

        public ICollection<CompareRuleConfig> RuleConfigs { get; set; } = new List<CompareRuleConfig>();

        public ICollection<CompareMasterData> MasterDataRows { get; set; } = new List<CompareMasterData>();
    }
}

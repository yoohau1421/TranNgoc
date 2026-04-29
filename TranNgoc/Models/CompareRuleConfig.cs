using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TranNgoc_BE.Models
{
    public class CompareRuleConfig
    {
        public long Id { get; set; }

        public long TemplateId { get; set; }

        public string ConfigKey { get; set; } = string.Empty;

        public string ConfigValue { get; set; } = string.Empty;

        public string DataType { get; set; } = "text";

        public bool IsActive { get; set; } = true;
        public CompareTemplate Template { get; set; } = null!;
    }
}

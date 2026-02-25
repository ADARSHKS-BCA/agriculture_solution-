using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Agriculture.Models
{
    [Table("scans")]
    public class ScanModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("risk_level")]
        public string RiskLevel { get; set; } = string.Empty;

        [Column("crop_type")]
        public string CropType { get; set; } = string.Empty;

        [Column("result_summary")]
        public string? ResultSummary { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}

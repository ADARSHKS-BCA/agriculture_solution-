using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Agriculture.Models
{
    [Table("analysis_history")]
    public class AnalysisHistoryModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("crop")]
        public string Crop { get; set; } = string.Empty;

        [Column("disease")]
        public string Disease { get; set; } = string.Empty;

        [Column("confidence")]
        public double Confidence { get; set; }

        [Column("risk_level")]
        public string RiskLevel { get; set; } = string.Empty;

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace Agriculture.Models
{
    public class RiskResult
    {
        [JsonPropertyName("risk_analysis")]
        public RiskAnalysis Analysis { get; set; } = new();

        [JsonPropertyName("chart_data")]
        public ChartData Chart { get; set; } = new();

        [JsonPropertyName("final_confidence")]
        public double FinalConfidence { get; set; }

        [JsonPropertyName("disease_progression")]
        public string DiseaseProgression { get; set; } = string.Empty;

        [JsonPropertyName("estimated_yield_impact")]
        public string EstimatedYieldImpact { get; set; } = string.Empty;
    }

    public class RiskAnalysis
    {
        [JsonPropertyName("risk_score")]
        public double RiskScore { get; set; }

        [JsonPropertyName("risk_level")]
        public string RiskLevel { get; set; } = string.Empty;

        [JsonPropertyName("components")]
        public RiskComponents Components { get; set; } = new();
    }

    public class RiskComponents
    {
        [JsonPropertyName("infection_factor")]
        public double InfectionFactor { get; set; }

        [JsonPropertyName("weather_factor")]
        public double WeatherFactor { get; set; }

        [JsonPropertyName("severity_factor")]
        public double SeverityFactor { get; set; }
    }

    public class ChartData
    {
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("values")]
        public List<int> Values { get; set; } = new();
    }
}

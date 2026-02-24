namespace Agriculture.Models;

public class AnalysisResultViewModel
{
    // Image & Input
    public string ImagePath { get; set; } = string.Empty;
    public string CropName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    // Disease Detection
    public string DiseaseName { get; set; } = string.Empty;
    public double Confidence { get; set; }

    // Weather
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Rainfall { get; set; }
    public double WindSpeed { get; set; }

    // Risk
    public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
    public string RiskExplanation { get; set; } = string.Empty;

    // Treatment
    public List<string> OrganicTreatment { get; set; } = new();
    public List<string> ChemicalTreatment { get; set; } = new();
    public List<string> PreventiveMeasures { get; set; } = new();

    // Error / Warning
    public string? InferenceError { get; set; }

    // Helpers
    public string ConfidenceColor =>
        Confidence >= 80 ? "success" :
        Confidence >= 50 ? "warning" : "danger";

    public string RiskColor =>
        RiskLevel switch
        {
            "Low" => "success",
            "Medium" => "warning",
            "High" => "danger",
            _ => "secondary"
        };

    public int RiskPercent =>
        RiskLevel switch
        {
            "Low" => 30,
            "Medium" => 60,
            "High" => 90,
            _ => 0
        };
}

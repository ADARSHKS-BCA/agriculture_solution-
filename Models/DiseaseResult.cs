namespace Agriculture.Models
{
    /// <summary>
    /// Input constraints and detection parameters passed into the Risk Engine.
    /// Typically populated by parsing the response from the OpenCV/FastAPI backend.
    /// </summary>
    public class DiseaseResult
    {
        public string Crop { get; set; } = string.Empty;
        
        public string Disease { get; set; } = "Healthy";
        
        /// <summary>
        /// Ranges from 0.0 to 1.0 (e.g., 0.15 for 15% infected leaf area).
        /// </summary>
        public double InfectionPercentage { get; set; }

        public int SpotCount { get; set; }
        
        /// <summary>
        /// Base ML prediction confidence (0.0 to 1.0) before environmental constraints.
        /// </summary>
        public double BaseConfidence { get; set; }
        
        /// <summary>
        /// Modifier based on weather conditions (capped between 0.0 and 0.25).
        /// </summary>
        public double WeatherModifier { get; set; }

        /// <summary>
        /// Textual representation of severity (Healthy, Mild, Moderate, Severe).
        /// </summary>
        public string Severity { get; set; } = "Healthy";

        /// <summary>
        /// Image blur penalty to reduce confidence if the image is poor quality.
        /// </summary>
        public double BlurPenalty { get; set; }
    }
}

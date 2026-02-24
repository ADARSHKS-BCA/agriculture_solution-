using System;
using System.Collections.Generic;
using Agriculture.Models;

namespace Agriculture.Services
{
    public class RiskEngine : IRiskEngine
    {
        public RiskResult CalculateRisk(DiseaseResult input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // 1. Calculate the Three Core Factors
            // Ensure infection is bounded between 0 and 1
            double safeInfection = Math.Max(0.0, Math.Min(input.InfectionPercentage, 1.0));
            double infectionFactor = safeInfection * 0.5;

            // Ensure weather modifier is bounded between 0 and 0.25 
            // Note: The original prompt requested 0-0.25 range for the weather_modifier specifically for this calculation.
            double safeWeather = Math.Max(0.0, Math.Min(input.WeatherModifier, 0.25));
            double weatherFactor = safeWeather * 0.3;

            // Get standard constraint weights based on textual severity
            double severityWeight = GetSeverityWeight(input.Severity);
            double severityFactor = severityWeight * 0.2;

            // 2. Risk Score & Level Output
            double riskScore = infectionFactor + weatherFactor + severityFactor;
            string riskLevel = "Low";
            
            if (riskScore > 0.7)
                riskLevel = "High";
            else if (riskScore >= 0.4)
                riskLevel = "Medium";

            // 3. Confidence Adjustment Logic
            // Final Confidence = Base Confidence + Weather Modifier - Blur Penalty
            double finalConfidence = input.BaseConfidence + input.WeatherModifier - input.BlurPenalty;
            
            // Constrain between 0.30 and 0.92 per requirements
            finalConfidence = Math.Max(0.30, Math.Min(finalConfidence, 0.92));

            // 4. Progession and Yield Impact
            string progression = "None";
            string yieldImpact = "0%";
            
            if (!input.Disease.Equals("Healthy", StringComparison.OrdinalIgnoreCase))
            {
                if (safeInfection < 0.15)
                {
                    progression = "Early";
                    yieldImpact = "5-10%";
                }
                else if (safeInfection <= 0.35)
                {
                    progression = "Mid";
                    yieldImpact = "10-25%";
                }
                else
                {
                    progression = "Advanced";
                    yieldImpact = "25-50%";
                }
            }

            // 5. Build Chart-Ready Output Dictionary
            // Values are percentage-based (0-100) for UI charting tools like Chart.js
            int infChartVal = (int)Math.Round(infectionFactor * 100);
            int weatherChartVal = (int)Math.Round(weatherFactor * 100);
            int sevChartVal = (int)Math.Round(severityFactor * 100);

            var chartData = new ChartData
            {
                Labels = new List<string> { "Infection", "Weather Impact", "Severity" },
                Values = new List<int> { infChartVal, weatherChartVal, sevChartVal }
            };

            var analysis = new RiskAnalysis
            {
                RiskScore = Math.Round(riskScore, 2),
                RiskLevel = riskLevel,
                Components = new RiskComponents
                {
                    InfectionFactor = Math.Round(infectionFactor, 2),
                    WeatherFactor = Math.Round(weatherFactor, 2),
                    SeverityFactor = Math.Round(severityFactor, 2)
                }
            };

            return new RiskResult
            {
                Analysis = analysis,
                Chart = chartData,
                FinalConfidence = Math.Round(finalConfidence, 2),
                DiseaseProgression = progression,
                EstimatedYieldImpact = yieldImpact
            };
        }

        private double GetSeverityWeight(string severityType)
        {
            if (string.IsNullOrWhiteSpace(severityType)) return 0.1;

            return severityType.ToLowerInvariant() switch
            {
                "severe" => 1.0,
                "moderate" => 0.7,
                "mild" => 0.4,
                "healthy" => 0.1,
                _ => 0.4 // Default to mild if unknown
            };
        }
    }
}

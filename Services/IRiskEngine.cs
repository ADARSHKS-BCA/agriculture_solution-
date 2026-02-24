using Agriculture.Models;

namespace Agriculture.Services
{
    public interface IRiskEngine
    {
        /// <summary>
        /// Calculates the final dynamic risk, confidence, disease progression, 
        /// and builds the chart-ready output for the frontend visualization.
        /// </summary>
        /// <param name="input">The base detection results</param>
        /// <returns>A comprehensive RiskResult containing data ready for UI</returns>
        RiskResult CalculateRisk(DiseaseResult input);
    }
}

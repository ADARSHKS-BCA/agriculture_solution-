using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agriculture.Services;

/// <summary>
/// Service that communicates with the FastAPI OpenCV backend for disease detection.
/// </summary>
public sealed class DiseaseDetectionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiseaseDetectionService> _logger;
    private const string BackendUrl = "http://localhost:8000/predict";

    public DiseaseDetectionService(HttpClient httpClient, ILogger<DiseaseDetectionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Runs inference on the uploaded image file via the FastAPI backend.
    /// Returns a tuple with the prediction results including weather analysis.
    /// </summary>
    public async Task<(string Disease, float Confidence, string RiskLevel, string RiskExplanation, FastApiResponse? FullResult, bool Success, string? Error)> PredictAsync(IFormFile imageFile, string cropType, string city, double? latitude = null, double? longitude = null)
    {
        if (imageFile == null || imageFile.Length == 0)
            return (string.Empty, 0f, string.Empty, string.Empty, null, false, "No image file was provided.");

        try
        {
            using var content = new MultipartFormDataContent();
            
            // Add crop type and city
            content.Add(new StringContent(cropType), "crop");
            content.Add(new StringContent(string.IsNullOrWhiteSpace(city) ? "Unknown" : city), "city");
            
            // Add coordinates if available
            if (latitude.HasValue && longitude.HasValue)
            {
                content.Add(new StringContent(latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "latitude");
                content.Add(new StringContent(longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "longitude");
            }

            // Add image file
            using var stream = imageFile.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(fileContent, "file", imageFile.FileName);

            _logger.LogInformation("Sending image to OpenCV backend for {CropType} at {City} [Lat: {Lat}, Lon: {Lon}]...", cropType, city, latitude, longitude);
            var response = await _httpClient.PostAsync(BackendUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Backend returned error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return (string.Empty, 0f, string.Empty, string.Empty, null, false, "Failed to analyze the image through the backend service.");
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FastApiResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
            {
                return (string.Empty, 0f, string.Empty, string.Empty, null, false, "Invalid response from the backend service.");
            }

            // The backend returns confidence as a ratio (e.g., 0.78), we convert to percentage for the UI
            var confidencePercent = (float)(result.Confidence * 100);

            _logger.LogInformation("Prediction: {Disease} ({Confidence:F1}%). Risk: {RiskLevel}", result.Disease, confidencePercent, result.Risk_Level);
            
            return (result.Disease ?? "Unknown", confidencePercent, result.Risk_Level ?? "Medium", result.Environmental_Explanation ?? "", result, true, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not connect to the OpenCV backend at {Url}. Ensure the Python server is running.", BackendUrl);
            return (string.Empty, 0f, string.Empty, string.Empty, null, false, "Could not connect to the local inference backend. Ensure it is running.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during inference request");
            return (string.Empty, 0f, string.Empty, string.Empty, null, false, "An error occurred while analyzing the image. Please try again.");
        }
    }

    public class FastApiResponse
    {
        [JsonPropertyName("crop")]
        public string? Crop { get; set; }
        
        [JsonPropertyName("disease")]
        public string? Disease { get; set; }
        
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
        
        [JsonPropertyName("severity")]
        public string? Severity { get; set; }
        
        [JsonPropertyName("risk_level")]
        public string? Risk_Level { get; set; }
        
        [JsonPropertyName("environmental_explanation")]
        public string? Environmental_Explanation { get; set; }
        
        [JsonPropertyName("weather_analysis")]
        public WeatherAnalysis? Weather_Analysis { get; set; }
    }
    
    public class WeatherAnalysis
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
        
        [JsonPropertyName("humidity")]
        public double? Humidity { get; set; }
        
        [JsonPropertyName("rainfall")]
        public double? Rainfall { get; set; }
        
        [JsonPropertyName("wind_speed")]
        public double? Wind_Speed { get; set; }
        
        [JsonPropertyName("climate_risk_level")]
        public string? Climate_Risk_Level { get; set; }
        
        [JsonPropertyName("risk_modifier")]
        public double? Risk_Modifier { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}

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
    /// Returns a tuple with the prediction results.
    /// </summary>
    public async Task<(string Disease, float Confidence, string RiskLevel, string RiskExplanation, bool Success, string? Error)> PredictAsync(IFormFile imageFile, string cropType)
    {
        if (imageFile == null || imageFile.Length == 0)
            return (string.Empty, 0f, string.Empty, string.Empty, false, "No image file was provided.");

        try
        {
            using var content = new MultipartFormDataContent();
            
            // Add crop type
            content.Add(new StringContent(cropType), "crop");

            // Add image file
            using var stream = imageFile.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(fileContent, "file", imageFile.FileName);

            _logger.LogInformation("Sending image to OpenCV backend for {CropType}...", cropType);
            var response = await _httpClient.PostAsync(BackendUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Backend returned error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return (string.Empty, 0f, string.Empty, string.Empty, false, "Failed to analyze the image through the backend service.");
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FastApiResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
            {
                return (string.Empty, 0f, string.Empty, string.Empty, false, "Invalid response from the backend service.");
            }

            // The backend returns confidence as a ratio (e.g., 0.78), we convert to percentage for the UI
            var confidencePercent = (float)(result.Confidence * 100);

            _logger.LogInformation("Prediction: {Disease} ({Confidence:F1}%). Risk: {RiskLevel}", result.Disease, confidencePercent, result.Risk_Level);
            
            return (result.Disease ?? "Unknown", confidencePercent, result.Risk_Level ?? "Medium", result.Environmental_Explanation ?? "", true, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not connect to the OpenCV backend at {Url}. Ensure the Python server is running.", BackendUrl);
            return (string.Empty, 0f, string.Empty, string.Empty, false, "Could not connect to the local inference backend. Ensure it is running.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during inference request");
            return (string.Empty, 0f, string.Empty, string.Empty, false, "An error occurred while analyzing the image. Please try again.");
        }
    }

    private class FastApiResponse
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
    }
}

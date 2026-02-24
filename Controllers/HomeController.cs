using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Agriculture.Models;
using Agriculture.Services;

namespace Agriculture.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly DiseaseDetectionService _diseaseService;

    public HomeController(
        ILogger<HomeController> logger,
        IWebHostEnvironment env,
        DiseaseDetectionService diseaseService)
    {
        _logger = logger;
        _env = env;
        _diseaseService = diseaseService;
    }

    public IActionResult Index()
    {
        return View(new CropAnalysisViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(CropAnalysisViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        // Save uploaded image
        string imagePath = string.Empty;
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            if (model.ImageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "File size must be less than 5 MB.");
                return View("Index", model);
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await model.ImageFile.CopyToAsync(stream);

            imagePath = $"/uploads/{fileName}";
        }

        try
        {
            // Run inference via the FastAPI Backend
            var (disease, confidence, riskLevel, riskExplanation, success, error) = await _diseaseService.PredictAsync(model.ImageFile!, model.CropType ?? "Unknown");

            AnalysisResultViewModel result;

            if (success)
            {
                // Build result with real AI prediction
                result = BuildResult(model, imagePath, disease, confidence, riskLevel, riskExplanation);
            }
            else
            {
                // Model not available — fall back to mock data with warning
                _logger.LogWarning("AI inference failed: {Error}. Using mock data.", error);
                result = BuildMockResult(model, imagePath);
                result.InferenceError = error;
            }

            TempData["AnalysisResult"] = JsonSerializer.Serialize(result);
            TempData["AnalysisSuccess"] = "true";
            return RedirectToAction("Result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during analysis");
            TempData["AnalysisError"] = "An unexpected error occurred. Please try again.";
            return RedirectToAction("Index");
        }
    }

    public IActionResult Result()
    {
        var json = TempData["AnalysisResult"] as string;
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Index");

        var result = JsonSerializer.Deserialize<AnalysisResultViewModel>(json);
        if (result == null)
            return RedirectToAction("Index");

        return View(result);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // ----------------------------------------------------------------
    //  Result builders
    // ----------------------------------------------------------------

    /// <summary>
    /// Builds AnalysisResultViewModel using real AI prediction.
    /// Disease-specific risk, weather, and treatment data are mapped by keyword matching.
    /// </summary>
    private AnalysisResultViewModel BuildResult(CropAnalysisViewModel input, string imagePath, string disease, float confidence, string riskLevel, string riskExplanation)
    {
        var region = string.IsNullOrWhiteSpace(input.State)
            ? input.Country
            : $"{input.State}, {input.Country}";

        var diseaseLower = disease.ToLowerInvariant();

        // Map treatment recommendations by disease type
        var (organic, chemical, preventive) = GetTreatmentRecommendations(diseaseLower);

        return new AnalysisResultViewModel
        {
            ImagePath = imagePath,
            CropName = input.CropType ?? "Unknown",
            Region = region,
            DiseaseName = disease,
            Confidence = Math.Round(confidence, 1),
            Temperature = 28.5,
            Humidity = 78,
            Rainfall = 12.3,
            WindSpeed = 14.2,
            RiskLevel = string.IsNullOrWhiteSpace(riskLevel) ? "Medium" : riskLevel,
            RiskExplanation = string.IsNullOrWhiteSpace(riskExplanation) ? "Results retrieved from backend successfully." : riskExplanation,
            OrganicTreatment = organic,
            ChemicalTreatment = chemical,
            PreventiveMeasures = preventive
        };
    }

    /// <summary>
    /// Fallback mock result when the AI model is not available.
    /// </summary>
    private AnalysisResultViewModel BuildMockResult(CropAnalysisViewModel input, string imagePath)
    {
        var diseases = new Dictionary<string, (string Disease, double Confidence, string Risk, string RiskExplanation)>
        {
            ["Tomato"] = ("Early Blight (Alternaria solani)", 92.4, "High",
                "High humidity (78%) combined with warm temperatures (28°C) create optimal conditions for Alternaria fungal spore germination and rapid spread across foliage."),
            ["Potato"] = ("Late Blight (Phytophthora infestans)", 87.6, "High",
                "Cool moist conditions with temperatures between 15–25°C and prolonged leaf wetness favor rapid sporangia production and systemic infection."),
            ["Corn"] = ("Northern Corn Leaf Blight", 78.3, "Medium",
                "Moderate humidity and warm days promote lesion expansion. Risk increases with consecutive wet nights."),
            ["Wheat"] = ("Wheat Rust (Puccinia triticina)", 85.1, "Medium",
                "Moderate temperatures (15–22°C) with intermittent rain creates favorable conditions for rust urediniospore dispersal."),
            ["Rice"] = ("Rice Blast (Magnaporthe oryzae)", 91.0, "High",
                "High nitrogen fertilization combined with prolonged leaf wetness and temperatures of 25–28°C significantly increase blast incidence.")
        };

        var crop = input.CropType ?? "Tomato";
        var (disease, confidence, risk, riskExplain) = diseases.ContainsKey(crop) ? diseases[crop] : diseases["Tomato"];

        var region = string.IsNullOrWhiteSpace(input.State)
            ? input.Country
            : $"{input.State}, {input.Country}";

        return new AnalysisResultViewModel
        {
            ImagePath = imagePath,
            CropName = crop,
            Region = region,
            DiseaseName = disease,
            Confidence = confidence,
            Temperature = 28.5,
            Humidity = 78,
            Rainfall = 12.3,
            WindSpeed = 14.2,
            RiskLevel = risk,
            RiskExplanation = riskExplain,
            OrganicTreatment = new List<string>
            {
                "Apply neem oil spray (2–3%) on affected foliage every 7 days",
                "Use Trichoderma viride bio-fungicide as a soil drench",
                "Spray Bacillus subtilis-based bio-pesticide on leaves",
                "Apply compost tea foliar spray to boost plant immunity",
                "Introduce beneficial mycorrhizal fungi to the root zone"
            },
            ChemicalTreatment = new List<string>
            {
                "Apply Mancozeb 75% WP at 2.5 g/L at first sign of symptoms",
                "Use Chlorothalonil fungicide spray at 2 g/L every 10–14 days",
                "Apply Azoxystrobin (Amistar) at 1 mL/L for systemic protection",
                "Copper oxychloride 50% WP at 3 g/L as a preventive measure"
            },
            PreventiveMeasures = new List<string>
            {
                "Practice crop rotation with non-host crops for 2–3 seasons",
                "Remove and destroy infected plant debris immediately",
                "Ensure proper plant spacing for adequate air circulation",
                "Use drip irrigation to minimize leaf wetness duration",
                "Select certified disease-resistant seed varieties",
                "Apply mulch to prevent soil-borne spore splash onto leaves"
            }
        };
    }

    /// <summary>
    /// Returns treatment recommendations based on disease keywords.
    /// </summary>
    private (List<string> Organic, List<string> Chemical, List<string> Preventive) GetTreatmentRecommendations(string diseaseLower)
    {
        if (diseaseLower.Contains("healthy"))
        {
            return (
                new List<string> { "No treatment needed — plant is healthy", "Continue regular organic fertilization", "Apply compost mulch for soil health" },
                new List<string> { "No chemical treatment required" },
                new List<string> { "Maintain regular crop monitoring schedule", "Practice crop rotation", "Ensure proper irrigation and drainage", "Use certified disease-free seeds" }
            );
        }

        if (diseaseLower.Contains("blight"))
        {
            return (
                new List<string>
                {
                    "Apply neem oil spray (2–3%) on affected foliage every 7 days",
                    "Use Trichoderma viride bio-fungicide as a soil drench",
                    "Spray Bacillus subtilis-based bio-pesticide on leaves",
                    "Apply compost tea foliar spray to boost plant immunity"
                },
                new List<string>
                {
                    "Apply Mancozeb 75% WP at 2.5 g/L at first sign of symptoms",
                    "Use Chlorothalonil fungicide spray at 2 g/L every 10–14 days",
                    "Apply Azoxystrobin (Amistar) at 1 mL/L for systemic protection",
                    "Copper oxychloride 50% WP at 3 g/L as a preventive measure"
                },
                new List<string>
                {
                    "Practice crop rotation with non-host crops for 2–3 seasons",
                    "Remove and destroy infected plant debris immediately",
                    "Ensure proper plant spacing for adequate air circulation",
                    "Use drip irrigation to minimize leaf wetness duration",
                    "Select certified disease-resistant seed varieties"
                }
            );
        }

        if (diseaseLower.Contains("rust"))
        {
            return (
                new List<string>
                {
                    "Apply sulfur-based organic fungicide at early onset",
                    "Spray neem oil (2%) as a preventive measure",
                    "Use Bacillus pumilus-based bio-fungicide"
                },
                new List<string>
                {
                    "Apply Propiconazole 25% EC at 1 mL/L at first sign",
                    "Use Tebuconazole fungicide spray at 1.5 mL/L",
                    "Apply Mancozeb as a preventive broad-spectrum spray"
                },
                new List<string>
                {
                    "Plant rust-resistant crop varieties",
                    "Avoid overhead irrigation",
                    "Remove volunteer and alternate host plants",
                    "Monitor fields regularly during humid conditions"
                }
            );
        }

        if (diseaseLower.Contains("spot"))
        {
            return (
                new List<string>
                {
                    "Apply copper-based organic fungicide (Bordeaux mixture)",
                    "Use neem oil spray (2–3%) weekly",
                    "Spray potassium bicarbonate solution"
                },
                new List<string>
                {
                    "Apply Chlorothalonil at 2 g/L every 7–10 days",
                    "Use Copper hydroxide 77% WP at 2.5 g/L",
                    "Apply Mancozeb 75% WP as preventive spray"
                },
                new List<string>
                {
                    "Remove and destroy affected leaves promptly",
                    "Avoid wetting foliage during irrigation",
                    "Ensure adequate plant spacing for airflow",
                    "Practice crop rotation with non-solanaceous crops"
                }
            );
        }

        if (diseaseLower.Contains("mildew") || diseaseLower.Contains("mold"))
        {
            return (
                new List<string>
                {
                    "Apply potassium bicarbonate spray (1 tbsp per gallon)",
                    "Use milk spray (40% milk, 60% water) weekly",
                    "Apply sulfur-based organic fungicide",
                    "Spray neem oil as a preventive measure"
                },
                new List<string>
                {
                    "Apply Myclobutanil fungicide at recommended rate",
                    "Use Triadimefon as a systemic treatment",
                    "Apply Sulfur WP at 3 g/L as preventive spray"
                },
                new List<string>
                {
                    "Improve air circulation by proper pruning",
                    "Avoid overhead watering",
                    "Remove heavily infected plant material",
                    "Select powdery mildew-resistant varieties"
                }
            );
        }

        if (diseaseLower.Contains("virus") || diseaseLower.Contains("curl") || diseaseLower.Contains("mosaic"))
        {
            return (
                new List<string>
                {
                    "Remove and destroy virus-infected plants immediately",
                    "Apply neem oil to control insect vectors (whiteflies/aphids)",
                    "Use reflective mulch to deter aphid/whitefly landing"
                },
                new List<string>
                {
                    "Apply Imidacloprid to control insect vectors",
                    "Use Thiamethoxam for whitefly control",
                    "No direct chemical cure for viral infections exists"
                },
                new List<string>
                {
                    "Use virus-free certified seedlings",
                    "Control insect vectors with sticky traps",
                    "Plant resistant varieties when available",
                    "Maintain weed-free field borders to reduce vector habitats"
                }
            );
        }

        // Default / generic
        return (
            new List<string>
            {
                "Apply neem oil spray (2–3%) on affected areas",
                "Use Trichoderma-based bio-fungicide as soil treatment",
                "Spray Bacillus subtilis-based bio-pesticide",
                "Apply compost tea foliar spray for plant immunity"
            },
            new List<string>
            {
                "Apply Mancozeb 75% WP at 2.5 g/L at first symptoms",
                "Use a broad-spectrum systemic fungicide as directed",
                "Copper oxychloride 50% WP at 3 g/L as preventive"
            },
            new List<string>
            {
                "Practice crop rotation for 2–3 seasons",
                "Remove and destroy infected plant material",
                "Ensure proper spacing and air circulation",
                "Use drip irrigation to reduce leaf wetness",
                "Plant disease-resistant varieties"
            }
        );
    }
}

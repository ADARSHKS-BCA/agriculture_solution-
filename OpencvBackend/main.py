import cv2
import numpy as np
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
import io

# Initialize FastAPI App
app = FastAPI(title="Crop Disease Detection (Rule-Based)")

# Allow CORS for easy frontend integration
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

def analyze_image_quality(image_gray):
    """
    Calculates blur score and brightness level of the image.
    Uses Laplacian variance to quantify blurriness.
    """
    # Laplacian variance for blur detection
    blur_score = cv2.Laplacian(image_gray, cv2.CV_64F).var()
    
    # Average pixel intensity for brightness
    brightness = np.mean(image_gray)
    if brightness < 80:
        brightness_level = "Dark"
    elif brightness > 200:
        brightness_level = "Bright"
    else:
        brightness_level = "Normal"
        
    return blur_score, brightness_level

def get_base_confidence(blur_score, brightness_level):
    """
    Computes a base confidence score adjusted by image quality.
    Confidence increases if clarity is high, decreases if blurry/poorly lit.
    Ceiled at 0.92 per requirements.
    """
    confidence = 0.85
    
    # Adjust for blur
    if blur_score < 50: # Very blurry
        confidence -= 0.25
    elif blur_score < 100: # Somewhat blurry
        confidence -= 0.10
    elif blur_score > 300: # Very sharp
        confidence += 0.05
        
    # Adjust for brightness
    if brightness_level == "Dark":
        confidence -= 0.15
    elif brightness_level == "Bright":
        confidence -= 0.10
        
    # Cap confidence constraints: min 0.40, max 0.92
    return min(max(confidence, 0.40), 0.92)

def determine_severity(infection_percentage):
    """
    Determines severity level based on infection percentage.
    - <15% infection -> Low
    - 15-35% infection -> Moderate
    - >35% infection -> Severe
    """
    if infection_percentage < 0.15:
        return "Low"
    elif infection_percentage <= 0.35:
        return "Moderate"
    else:
        return "Severe"

def process_crop_disease(image, crop_type):
    """
    Applies image processing and rule-based logic to detect disease.
    """
    # 1. Preprocess the image
    resized = cv2.resize(image, (500, 500))  # Standardize size for consistent area calculation
    gray = cv2.cvtColor(resized, cv2.COLOR_BGR2GRAY)
    
    # Assess Image Quality
    blur_score, brightness_level = analyze_image_quality(gray)
    base_confidence = get_base_confidence(blur_score, brightness_level)
    
    # Denoise / Blur reduction for better masking
    denoised = cv2.GaussianBlur(resized, (5, 5), 0)
    
    # Convert to HSV color space
    hsv = cv2.cvtColor(denoised, cv2.COLOR_BGR2HSV)
    
    # 2. Extract specific color regions
    # Yellow chlorosis masking (detects yellowing leaves)
    lower_yellow = np.array([15, 50, 50])
    upper_yellow = np.array([35, 255, 255])
    mask_yellow = cv2.inRange(hsv, lower_yellow, upper_yellow)
    
    # Brown lesions masking (detects dead tissue/spots)
    lower_brown = np.array([10, 40, 20])
    upper_brown = np.array([20, 255, 200])
    mask_brown = cv2.inRange(hsv, lower_brown, upper_brown)
    
    # Combine masks to get total infected region
    mask_infected = cv2.bitwise_or(mask_yellow, mask_brown)
    
    # Clean up mask noise using Morphological operations
    kernel = np.ones((5, 5), np.uint8)
    mask_infected = cv2.morphologyEx(mask_infected, cv2.MORPH_OPEN, kernel, iterations=1)
    mask_infected = cv2.morphologyEx(mask_infected, cv2.MORPH_CLOSE, kernel, iterations=1)
    
    # 3. Calculate statistics
    total_pixels = resized.shape[0] * resized.shape[1]
    infected_pixels = cv2.countNonZero(mask_infected)
    yellow_pixels = cv2.countNonZero(mask_yellow)
    brown_pixels = cv2.countNonZero(mask_brown)
    
    infection_ratio = infected_pixels / total_pixels
    yellow_ratio = yellow_pixels / total_pixels
    brown_ratio = brown_pixels / total_pixels
    
    # Find contours to count spots and measure sizes using structural detection
    contours, _ = cv2.findContours(mask_infected, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    
    # Filter small noise contours (area < 15)
    valid_contours = [c for c in contours if cv2.contourArea(c) > 15]
    spot_count = len(valid_contours)
    
    avg_lesion_size = 0
    max_lesion_size = 0
    if spot_count > 0:
        areas = [cv2.contourArea(c) for c in valid_contours]
        avg_lesion_size = sum(areas) / len(areas)
        max_lesion_size = max(areas)
        
    # 4. Apply Rule-Based Logic
    disease = "Unknown or Unrecognized"
    risk_level = "Unknown"
    env_explanation = "Requires further analysis."
    
    # Healthy Check First
    if infection_ratio < 0.05:
        disease = "Healthy"
        risk_level = "Low"
        env_explanation = "No significant disease symptoms detected. Maintain current environmental controls."
    else:
        # Crop-Specific Rules
        crop = crop_type.title()
        
        if crop == "Tomato":
            if infection_ratio > 0.30 and spot_count > 20:
                disease = "Septoria Leaf Spot"
                risk_level = "Medium"
                env_explanation = "High humidity and prolonged leaf wetness increase fungal spread."
            elif infection_ratio > 0.35 and max_lesion_size > 500:
                disease = "Early Blight"
                risk_level = "High"
                env_explanation = "Favored by warm temperatures and high humidity/frequent dew. Lesions exhibit concentric rings."
            elif yellow_ratio > 0.20 and brown_ratio < 0.05:
                disease = "Tomato Yellow Leaf Curl Virus"
                risk_level = "Severe"
                env_explanation = "Viral infection usually transmitted by whiteflies in warm conditions."
            else:
                disease = "Target Spot"
                risk_level = "Medium"
                env_explanation = "Moderate fungal activity detected, often associated with dense canopies."

        elif crop == "Potato":
            if infection_ratio > 0.30 and max_lesion_size > 800:
                disease = "Late Blight"
                risk_level = "Severe"
                env_explanation = "Cool, wet weather with high relative humidity drives rapid sporangia production."
            elif spot_count > 15 and brown_ratio > 0.10:
                disease = "Early Blight"
                risk_level = "Medium"
                env_explanation = "Alternaria fungus spreads efficiently in alternating wet and dry conditions."
            else:
                disease = "General Fungal Infection"
                risk_level = "Low"
                env_explanation = "Minor localized symptoms observed."

        elif crop == "Corn":
            if brown_ratio > 0.15 and max_lesion_size > 1000:
                disease = "Northern Leaf Blight"
                risk_level = "High"
                env_explanation = "Large cigar-shaped lesions occur in moderate temperatures with heavy dew."
            elif yellow_ratio > 0.10 and spot_count > 30:
                disease = "Common Rust"
                risk_level = "Medium"
                env_explanation = "Rust pustules develop heavily under high humidity, moderate temperatures."
            elif spot_count > 20 and avg_lesion_size < 300:
                disease = "Cercospora Leaf Spot (Gray Leaf Spot)"
                risk_level = "High"
                env_explanation = "Extended periods of damp, overcast conditions facilitate spread."
            else:
                disease = "Minor Leaf Spot"
                risk_level = "Low"
                env_explanation = "Slight environmental stress or minor fungal colonization."

        elif crop == "Wheat":
            if yellow_ratio > 0.15 and spot_count < 10:
                disease = "Yellow Rust (Stripe Rust)"
                risk_level = "Severe"
                env_explanation = "Cool, moist weather highly favorable for rapid stripe rust spread."
            elif brown_ratio > 0.10 and spot_count > 25:
                disease = "Brown Leaf Rust"
                risk_level = "Medium"
                env_explanation = "Warm days and dewy nights encourage pustule eruption."
            else:
                disease = "Unspecified Wheat Infection"
                risk_level = "Low"
                env_explanation = "Monitor closely for changes."

        elif crop == "Rice":
            if max_lesion_size > 600 and brown_ratio > 0.15:
                disease = "Rice Blast"
                risk_level = "Severe"
                env_explanation = "High nitrogen soils, prolonged leaf wetness, and cool nights promote severe blast outbreaks."
            elif spot_count > 25 and avg_lesion_size < 150:
                disease = "Brown Spot"
                risk_level = "Medium"
                env_explanation = "Often indicates soil nutrient deficiency combined with high humidity."
            else:
                disease = "Minor Discoloration"
                risk_level = "Low"
                env_explanation = "Could be caused by minor environmental stress or aging."
                
    severity_level = determine_severity(infection_ratio)
    
    # Final confidence adjustment based on clarity of rules
    final_confidence = base_confidence
    if disease == "Healthy":
        final_confidence = min(final_confidence + 0.05, 0.92)
    elif "Unspecified" in disease or "General" in disease or "Minor" in disease:
        final_confidence -= 0.10 # Lower confidence for generic buckets
    final_confidence = round(min(max(final_confidence, 0.40), 0.92), 2)

    # Compile the final structured JSON output
    return {
        "crop": crop_type.title(),
        "disease": disease,
        "confidence": final_confidence,
        "severity": severity_level,
        "infection_percentage": round(infection_ratio, 4),
        "spot_count": spot_count,
        "image_quality": {
            "blur_score": round(blur_score, 1),
            "brightness_level": brightness_level
        },
        "risk_level": risk_level,
        "environmental_explanation": env_explanation
    }

@app.post("/predict")
async def predict_disease(
    crop: str = Form(..., description="Crop type (e.g., Tomato, Potato, Corn, Wheat, Rice)"),
    file: UploadFile = File(..., description="Uploaded leaf image")
):
    try:
        # Read image bytes asynchronously
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        
        # Decode image using OpenCV
        image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if image is None:
            raise HTTPException(status_code=400, detail="Invalid image file provided.")
            
        # Process image and apply rules
        result = process_crop_disease(image, crop)
        
        return JSONResponse(content=result)
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    # Make sure to run the server on a specified port
    uvicorn.run(app, host="0.0.0.0", port=8000)

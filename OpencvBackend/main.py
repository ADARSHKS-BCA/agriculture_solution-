import cv2
import numpy as np
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
import time

from weather_service import get_weather_data
from image_processor import extract_visual_features
from risk_engine import evaluate_risk

app = FastAPI(title="Crop Disease Detection (Weather+Visual Rule-Based)")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

def process_crop_disease(image, crop_type, city, latitude, longitude):
    """
    Coordinates visual processing, weather fetching, and risk evaluation.
    """
    # 1. Visual Feature Extraction (OpenCV)
    visual_data = extract_visual_features(image, crop_type)
    
    # 2. Weather Data Fetching (Open-Meteo)
    weather_data, weather_error = get_weather_data(city, latitude=latitude, longitude=longitude)
    
    # 3. Merging Context through Risk Engine
    risk_data = evaluate_risk(
        disease=visual_data["disease"],
        infection_ratio=visual_data["infection_percentage"],
        spot_count=visual_data["spot_count"],
        blur_score=visual_data["blur_score"],
        base_confidence=visual_data["base_confidence"],
        weather_data=weather_data
    )
    
    # 4. Compile Output (matching the required JSON format)
    response = {
        "crop": crop_type.title(),
        "disease": visual_data["disease"],
        "confidence": risk_data["final_confidence"],
        "severity": visual_data["severity"],
        "infection_percentage": visual_data["infection_percentage"],
        "spot_count": visual_data["spot_count"],
        "image_quality": {
            "blur_score": round(visual_data["blur_score"], 1),
            "brightness_level": visual_data["brightness_level"]
        },
        "risk_level": risk_data["risk_level"],
        "environmental_explanation": risk_data["environmental_explanation"],
        "weather_analysis": {
            "latitude": weather_data["latitude"] if weather_data else None,
            "longitude": weather_data["longitude"] if weather_data else None,
            "temperature": weather_data["temperature"] if weather_data else None,
            "humidity": weather_data["humidity"] if weather_data else None,
            "rainfall": weather_data["rainfall"] if weather_data else None,
            "wind_speed": weather_data["wind_speed"] if weather_data else None,
            "climate_risk_level": risk_data["climate_risk_level"],
            "weather_modifier": risk_data["risk_modifier"],
            "error": weather_error
        }
    }
    
    return response

@app.post("/predict")
async def predict_disease(
    crop: str = Form(..., description="Crop type (e.g., Tomato, Potato, Corn, Wheat, Rice)"),
    city: str = Form("Unknown", description="City for weather API (optional)"),
    latitude: float = Form(None, description="Latitude from explicit geolocation"),
    longitude: float = Form(None, description="Longitude from explicit geolocation"),
    file: UploadFile = File(..., description="Uploaded leaf image")
):
    try:
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        
        image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if image is None:
            raise HTTPException(status_code=400, detail="Invalid image file provided.")
            
        result = process_crop_disease(image, crop, city, latitude, longitude)
        return JSONResponse(content=result)
        
    except Exception as e:
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)

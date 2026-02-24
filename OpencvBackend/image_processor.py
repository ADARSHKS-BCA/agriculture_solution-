import cv2
import numpy as np

def analyze_image_quality(image_gray):
    """
    Calculates blur score and brightness level of the image.
    Uses Laplacian variance to quantify blurriness.
    """
    blur_score = cv2.Laplacian(image_gray, cv2.CV_64F).var()
    brightness = np.mean(image_gray)
    
    if brightness < 80:
        brightness_level = "Low"
    elif brightness > 200:
        brightness_level = "High"
    else:
        brightness_level = "Normal"
        
    return blur_score, brightness_level

def get_base_confidence(blur_score, brightness_level):
    """
    Computes a base confidence score adjusted by image quality.
    """
    confidence = 0.85
    
    if blur_score < 50:
        confidence -= 0.25
    elif blur_score < 100:
        confidence -= 0.10
    elif blur_score > 300:
        confidence += 0.05
        
    if brightness_level == "Low":
        confidence -= 0.15
    elif brightness_level == "High":
        confidence -= 0.10
        
    return min(max(confidence, 0.40), 0.92)

def determine_severity(infection_percentage):
    """
    Determines severity level based on infection percentage.
    """
    if infection_percentage < 0.15:
        return "Low"
    elif infection_percentage <= 0.35:
        return "Moderate"
    else:
        return "Severe"

def extract_visual_features(image, crop_type):
    """
    Applies image processing to extract visual signs of disease.
    """
    resized = cv2.resize(image, (500, 500))
    gray = cv2.cvtColor(resized, cv2.COLOR_BGR2GRAY)
    
    blur_score, brightness_level = analyze_image_quality(gray)
    base_confidence = get_base_confidence(blur_score, brightness_level)
    
    denoised = cv2.GaussianBlur(resized, (5, 5), 0)
    hsv = cv2.cvtColor(denoised, cv2.COLOR_BGR2HSV)
    
    # Yellow chlorosis parameters
    lower_yellow = np.array([15, 50, 50])
    upper_yellow = np.array([35, 255, 255])
    mask_yellow = cv2.inRange(hsv, lower_yellow, upper_yellow)
    
    # Brown lesions parameters
    lower_brown = np.array([10, 40, 20])
    upper_brown = np.array([20, 255, 200])
    mask_brown = cv2.inRange(hsv, lower_brown, upper_brown)
    
    mask_infected = cv2.bitwise_or(mask_yellow, mask_brown)
    kernel = np.ones((5, 5), np.uint8)
    mask_infected = cv2.morphologyEx(mask_infected, cv2.MORPH_OPEN, kernel, iterations=1)
    mask_infected = cv2.morphologyEx(mask_infected, cv2.MORPH_CLOSE, kernel, iterations=1)
    
    total_pixels = resized.shape[0] * resized.shape[1]
    infected_pixels = cv2.countNonZero(mask_infected)
    yellow_pixels = cv2.countNonZero(mask_yellow)
    brown_pixels = cv2.countNonZero(mask_brown)
    
    infection_ratio = infected_pixels / total_pixels
    yellow_ratio = yellow_pixels / total_pixels
    brown_ratio = brown_pixels / total_pixels
    
    contours, _ = cv2.findContours(mask_infected, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    valid_contours = [c for c in contours if cv2.contourArea(c) > 15]
    spot_count = len(valid_contours)
    
    avg_lesion_size = 0
    max_lesion_size = 0
    if spot_count > 0:
        areas = [cv2.contourArea(c) for c in valid_contours]
        avg_lesion_size = sum(areas) / len(areas)
        max_lesion_size = max(areas)
        
    disease = identify_disease(crop_type, infection_ratio, yellow_ratio, brown_ratio, spot_count, avg_lesion_size, max_lesion_size)
    severity_level = determine_severity(infection_ratio)

    return {
        "disease": disease,
        "base_confidence": base_confidence,
        "severity": severity_level,
        "infection_percentage": round(infection_ratio, 4),
        "spot_count": spot_count,
        "blur_score": blur_score,
        "brightness_level": brightness_level
    }

def identify_disease(crop_type, infection_ratio, yellow_ratio, brown_ratio, spot_count, avg_lesion_size, max_lesion_size):
    """
    Core visual rule-based logic stripped of environmental statements.
    """
    if infection_ratio < 0.05:
        return "Healthy"
        
    crop = crop_type.title()
    
    if crop == "Tomato":
        if infection_ratio > 0.30 and spot_count > 20: return "Septoria Leaf Spot"
        elif infection_ratio > 0.35 and max_lesion_size > 500: return "Early Blight"
        elif yellow_ratio > 0.20 and brown_ratio < 0.05: return "Tomato Yellow Leaf Curl Virus"
        else: return "Target Spot"

    elif crop == "Potato":
        if infection_ratio > 0.30 and max_lesion_size > 800: return "Late Blight"
        elif spot_count > 15 and brown_ratio > 0.10: return "Early Blight"
        else: return "General Fungal Infection"

    elif crop == "Corn":
        if brown_ratio > 0.15 and max_lesion_size > 1000: return "Northern Leaf Blight"
        elif yellow_ratio > 0.10 and spot_count > 30: return "Common Rust"
        elif spot_count > 20 and avg_lesion_size < 300: return "Cercospora Leaf Spot (Gray Leaf Spot)"
        else: return "Minor Leaf Spot"

    elif crop == "Wheat":
        if yellow_ratio > 0.15 and spot_count < 10: return "Yellow Rust (Stripe Rust)"
        elif brown_ratio > 0.10 and spot_count > 25: return "Brown Leaf Rust"
        else: return "Unspecified Wheat Infection"

    elif crop == "Rice":
        if max_lesion_size > 600 and brown_ratio > 0.15: return "Rice Blast"
        elif spot_count > 25 and avg_lesion_size < 150: return "Brown Spot"
        else: return "Minor Discoloration"
        
    return "Unknown or Unrecognized"

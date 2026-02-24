def evaluate_risk(disease, infection_ratio, spot_count, blur_score, base_confidence, weather_data):
    """
    Evaluates weather impact on detected disease logically.
    """
    risk_level = "Unknown"
    env_explanation = "Requires further analysis."
    weather_modifier = 0.0
    weather_risk = "Low"

    # Weather thresholds
    humidity = weather_data.get("humidity", 0) if weather_data else 0
    rainfall = weather_data.get("rainfall", 0) if weather_data else 0
    temperature = weather_data.get("temperature", 0) if weather_data else 0

    high_humidity = humidity > 80
    high_rain = rainfall > 20.0
    extreme_heat = temperature > 35

    disease_lower = disease.lower()
    is_fungal = any(x in disease_lower for x in ["blight", "spot", "rust", "blast", "fungal", "mildew"])

    if weather_data and not weather_data.get("error"):
        if is_fungal and high_humidity:
            weather_modifier += 0.15
            weather_risk = "High"
            env_explanation = f"High humidity ({humidity}%) strongly promotes fungal disease spread."
        elif is_fungal and humidity < 40:
            weather_modifier -= 0.15 # Contradicts fungal
            weather_risk = "Low"
            env_explanation = f"Low humidity ({humidity}%) contradicts fungal spread, reducing confidence."
        
        if high_rain:
            weather_modifier += 0.10
            weather_risk = "High" if is_fungal else "Moderate"
            prefix = env_explanation + " " if weather_modifier > 0.10 else ""
            env_explanation = prefix + f"Heavy rainfall ({rainfall}mm) increases risk of waterborne/bacterial infections."
            
        if extreme_heat:
            if disease == "Healthy":
                weather_modifier -= 0.15
                weather_risk = "Moderate"
                env_explanation = f"Extreme heat ({temperature}°C) induces plant stress, contradicting healthy status."
            else:
                weather_modifier += 0.10
                weather_risk = "High"
                prefix = env_explanation + " " if weather_modifier > 0.10 else ""
                env_explanation = prefix + f"Extreme heat ({temperature}°C) weakens plant defenses."
    else:
        env_explanation = "Weather data unavailable for risk correlation."
        weather_risk = "Unknown"

    # Cap weather modifier
    weather_modifier = max(min(weather_modifier, 0.25), -0.25)

    # Base risk level mapping based on infection
    if disease == "Healthy":
        risk_level = "Low" if weather_risk != "Moderate" else "Medium"
    elif infection_ratio > 0.30 or weather_risk == "High":
        if disease != "Healthy":
            risk_level = "Severe"
    elif infection_ratio > 0.15:
        risk_level = "High"
    else:
        risk_level = "Medium"

    # Blur penalty 
    blur_penalty = 0.0
    if blur_score < 50:
        blur_penalty = 0.15
        
    final_confidence = base_confidence + weather_modifier - blur_penalty
    
    # Cap final confidence
    final_confidence = round(min(max(final_confidence, 0.30), 0.92), 2)

    return {
        "final_confidence": final_confidence,
        "risk_level": risk_level,
        "environmental_explanation": env_explanation,
        "climate_risk_level": weather_risk,
        "risk_modifier": round(weather_modifier, 2)
    }

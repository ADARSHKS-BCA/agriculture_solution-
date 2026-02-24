import requests
import logging

logger = logging.getLogger(__name__)

GEOCODING_URL = "https://geocoding-api.open-meteo.com/v1/search"
WEATHER_URL = "https://api.open-meteo.com/v1/forecast"

def get_weather_data(city: str, latitude: float = None, longitude: float = None):
    """
    Fetches real-time weather data using coordinates or city name via Open-Meteo.
    Returns: temperature, humidity, rainfall (last 24h), wind speed, and success status.
    """
    lat, lon = latitude, longitude

    try:
        # 1. Geocoding (only if lat/lon not explicitly provided)
        if lat is None or lon is None:
            if not city or city.strip().lower() == "unknown":
                logger.warning("No coordinates and city unknown. Skipping weather API.")
                return None, "Location not provided"

            geo_params = {
                "name": city,
                "count": 1,
                "language": "en",
                "format": "json"
            }
            geo_response = requests.get(GEOCODING_URL, params=geo_params, timeout=5)
            geo_response.raise_for_status()
            geo_data = geo_response.json()

            if not geo_data.get("results"):
                logger.warning(f"Could not find coordinates for city: {city}")
                return None, f"City '{city}' not found"

            location = geo_data["results"][0]
            lat, lon = location["latitude"], location["longitude"]

        # 2. Weather Fetch (Using lat/lon)
        # To get the "last 24 hours" of rainfall, we'll fetch the daily rain_sum for today.
        weather_params = {
            "latitude": lat,
            "longitude": lon,
            "current": "temperature_2m,relative_humidity_2m,wind_speed_10m",
            "daily": "rain_sum",
            "timezone": "auto"
        }
        
        weather_response = requests.get(WEATHER_URL, params=weather_params, timeout=5)
        weather_response.raise_for_status()
        weather_data = weather_response.json()
        
        current = weather_data.get("current", {})
        daily = weather_data.get("daily", {})
        
        # Extract today's rainfall (index 0 usually represents today in 'daily' response)
        rainfall_24h = 0.0
        if "rain_sum" in daily and len(daily["rain_sum"]) > 0:
            rainfall_24h = daily["rain_sum"][0]
            if rainfall_24h is None:
                rainfall_24h = 0.0
        
        return {
            "latitude": float(lat),
            "longitude": float(lon),
            "temperature": current.get("temperature_2m", 0.0),
            "humidity": current.get("relative_humidity_2m", 0.0),
            "rainfall": float(rainfall_24h),
            "wind_speed": current.get("wind_speed_10m", 0.0)
        }, None

    except requests.exceptions.RequestException as e:
        logger.error(f"Weather API request failed: {e}")
        return None, "Weather API timeout or connection error"
    except Exception as e:
        logger.error(f"Error processing weather data: {e}")
        return None, str(e)

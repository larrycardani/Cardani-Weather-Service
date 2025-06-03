import argparse
import json
import requests
import yaml

# This could be better protected.
MyApiKey = "d4c9d31d2b59d2f61c401d25e2133b45"

DegreeSymbol = "\u00B0"

"""
Fetches the current weather data for a given U.S. ZIP code using the OpenWeatherMap API.

Args:
    zipcode (str): The ZIP code for the weather data.
    units (str): The unit system to use for temperature. 
                 Accepts "fahrenheit" for imperial units and "celsius" (any other value defaults to metric) for metric.

Returns:
    dict or None: A dictionary containing the current temperature (rounded),
                  the unit system and whether rain is possible today.
                  Returns None if there is an error during the API request.

Note:
    This function assumes that the variable 'MyApiKey' contains a valid OpenWeatherMap API key.
"""
def http_get_current(zipcode, units):
    # Set up the unitParameter properly for the openweathermap API.
    unitParameter = "imperial" if units == "fahrenheit" else "metric"

    # Format the URL for the openweathermap API call.
    url = f"https://api.openweathermap.org/data/2.5/weather?zip={zipcode},us&units={unitParameter}&appid={MyApiKey}"

    # Call the weather API.    
    try:
        r = requests.get(url)
        r.raise_for_status()
        data = r.json()

        # Find the temperature and whether rain is possible. 
        temp = data['main']['temp']
        rain_possible = 'rain' in data or 'drizzle' in data.get('weather', [{}])[0].get('main', '').lower()

        # Format the response, rounding the temperature.
        response = {
            "zipcode": zipcode,
            "currentTemperature": round(temp),
            "units": units,
            "rainPossibleToday": rain_possible
        }
        return response
    
    except requests.RequestException as e:
        print(f"Error fetching data: {e}")
        return None

"""
Fetches the average weather data for a given U.S. ZIP code using the OpenWeatherMap API.

Args:
    zipcode (str): The ZIP code for the weather data.
    units (str): The unit system to use for temperature. 
                 Accepts "fahrenheit" for imperial units and "celsius" (any other value defaults to metric) for metric.
    timeperiod (int): The number of days for the average. Must be between 2 and 5 inclusive.

Returns:
    dict or None: A dictionary containing the average temperature (rounded),
                  the unit system and whether rain is possible durig this period.
                  Returns None if there is an error during the API request.

Note:
    This function assumes that the variable 'MyApiKey' contains a valid OpenWeatherMap API key.
"""
def http_get_average(zipcode, units, timeperiod):
    # Set up the unitParameter propery for the openweathermap API.
    unitParameter = "imperial" if units == "fahrenheit" else "metric"

    # Format the URL for the openweathermap API call.
    url = f"https://api.openweathermap.org/data/2.5/forecast?zip={zipcode},us&units={unitParameter}&appid={MyApiKey}"
    
    # Call the weather API.    
    try:
        r = requests.get(url)
        r.raise_for_status()
        data = r.json()

        # We get back a list of forecasts in 3 hour intervals (8 per day).
        # We only want timeperiod days worth of temperatures to average.
        forecasts = data['list'][:timeperiod * 8]
        temps = [entry['main']['temp'] for entry in forecasts]
        avg_temp = sum(temps) / len(temps)
        rain_possible = any('rain' in entry for entry in forecasts)

        # Format the response, rounding the temperature.
        response = {
            "zipcode": zipcode,
            "currentTemperature": round(avg_temp),
            "units": units,
            "rainPossibleToday": rain_possible
        }
        return response
    
    except requests.RequestException as e:
        print(f"Error fetching data: {e}")
        return None

def main():
    parser = argparse.ArgumentParser(description="Get weather info")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # Subparser for "get-current-weather"
    get_current_parser = subparsers.add_parser("get-current-weather", help="Get current weather info")
    get_current_parser.add_argument("zipcode", type=str, help="ZIP code")
    get_current_parser.add_argument("units", choices=["fahrenheit", "celsius"], help="Units")

    # Part of the instructions mention "table". This could be added if more details were provided.
    get_current_parser.add_argument("--output", choices=["json", "yaml", "text"], default="text", help="Output format")

    # Subparser for "get-average-weather"
    get_current_parser = subparsers.add_parser("get-average-weather", help="Get average weather info")
    get_current_parser.add_argument("zipcode", type=str, help="ZIP code")
    get_current_parser.add_argument("units", choices=["fahrenheit", "celsius"], help="Units")
    get_current_parser.add_argument("timePeriod", type=int, choices=range(2,6), help="Time Period")
    get_current_parser.add_argument("--output", choices=["json", "yaml", "text"], default="text", help="Output format")

    args = parser.parse_args()

    if args.command == "get-current-weather":
        # Get the current weather, based on the zip code and the units.
        response = http_get_current(args.zipcode, args.units)

        # Get F or C for units
        unitCharacter = "F" if args.units == "fahrenheit" else "C"

        if args.output == "json":
            print(json.dumps(response, indent=2))
        elif args.output == "yaml":
            # If we want zipcode and units to both have quotes, we could customize the dumper.
            print(yaml.dump(response, default_flow_style=False))
        else:
            print(f"Location: {response['zipcode']}")
            print(f"{response['currentTemperature']}{DegreeSymbol} {unitCharacter}")
            print(f"Rain Possible Today: {response['rainPossibleToday']}")
    elif args.command == "get-average-weather":
        # Get the average weather, based on the zip code, the units and the timePeriod number of days.
        response = http_get_average(args.zipcode, args.units, args.timePeriod)

        # Get F or C for units
        unitCharacter = "F" if args.units == "fahrenheit" else "C"

        if args.output == "json":
            print(json.dumps(response, indent=2))
        elif args.output == "yaml":
            # If we want zipcode and units to both have quotes, we could customize the dumper.
            print(yaml.dump(response, default_flow_style=False))
        else:
            print(f"Location: {response['zipcode']}")
            print(f"{response['currentTemperature']}{DegreeSymbol} {unitCharacter}")
            print(f"Rain Possible Soon: {response['rainPossibleToday']}")

if __name__ == "__main__":
    main()
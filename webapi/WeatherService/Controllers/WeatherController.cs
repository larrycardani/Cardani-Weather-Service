using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    /// <summary>
    /// Controller for retrieving weather data based on a U.S. ZIP code using the OpenWeatherMap API.
    /// </summary>
    /// <remarks>
    /// Provides two endpoints:
    /// 
    /// - GET /Weather/Current/{zipcode}?units={units}  
    ///   Retrieves the current weather conditions including temperature, location coordinates,
    ///   and whether rain is expected today.
    ///
    /// - GET /Weather/Average/{zipcode}?units={units}&timePeriod={days}
    ///   Retrieves the average temperature over the next few days and whether rain is expected during that time.
    ///
    /// Requires a valid API key defined as a constant in the controller.
    /// </remarks>
    public class WeatherController : ControllerBase
    {
        // For HTTP calls:
        private readonly HttpClient _httpClient;

        // Constant for the given API key
        private const string MyApiKey = "d4c9d31d2b59d2f61c401d25e2133b45";

        // Initializes the controller with an HttpClient instance for making API requests.
        public WeatherController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Endpoint for "Current/ZIPCODE"
        // units parameter comes from the query.
        // zipcode comes from the url itself.
        [HttpGet("Current/{zipcode}")]
        public async Task<IActionResult> GetCurrentWeather([FromRoute] string zipcode, [FromQuery] string units)
        {
            // Check for blank zipcode or invalid units.
            if (string.IsNullOrWhiteSpace(zipcode) || (units != "fahrenheit" && units != "celsius"))
            {
                return BadRequest("Invalid input.");
            }

            // Set up the unitParameter propery for the openweathermap API.
            string unitParameter = units == "fahrenheit" ? "imperial" : "metric";

            // Set up the URL with the proper zipcode, unitParameter and ApiKey.
            var url = $"https://api.openweathermap.org/data/2.5/weather?zip={zipcode},us&units={unitParameter}&appid={MyApiKey}";

            try
            {
                // Send the request and wait for the response.
                var response = await _httpClient.GetAsync(url);

                // Throw an exception if the response code is a failure.
                // Note that the API returns 404 if the location is not found, which seems to be the right code.
                // We could convert this to 400 if needed.
                response.EnsureSuccessStatusCode();

                // Get the JSON content.
                var json = await response.Content.ReadAsStringAsync();

                // Get the parsed results.
                using var doc = JsonDocument.Parse(json);

                // Start at the top level.
                var root = doc.RootElement;

                // Set up the results to be returned.
                var result = new
                {
                    currentTemperature = Math.Round(root.GetProperty("main").GetProperty("temp").GetDouble()),
                    unit = units == "fahrenheit" ? "F" : "C",
                    lat = root.GetProperty("coord").GetProperty("lat").GetDouble(),
                    lon = root.GetProperty("coord").GetProperty("lon").GetDouble(),
                    rainPossibleToday = root.TryGetProperty("rain", out _) // simple presence check
                };

                return Ok(result);
            }

            catch (HttpRequestException ex)
            {
                return StatusCode(503, $"Error retrieving current weather data: {ex.Message}");
            }

            catch (JsonException ex)
            {
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }
        }

        // Endpoint for "Average/ZIPCODE"
        // units and timePeriod parameters come from the query.
        // zipcode comes from the url itself.
        [HttpGet("Average/{zipcode}")]
        public async Task<IActionResult> GetAverageWeather([FromRoute] string zipcode, [FromQuery] string units, [FromQuery] int timePeriod)
        {
            // Check for blank zipcode or invalid units.
            if (string.IsNullOrWhiteSpace(zipcode) || (units != "fahrenheit" && units != "celsius") || timePeriod < 2 || timePeriod > 5)
            {
                return BadRequest("Invalid input.");
            }

            // Set up the unitParameter propery for the openweathermap API.
            string unitParameter = units == "fahrenheit" ? "imperial" : "metric";

            // Set up the URL with the proper zipcode, unitParameter and ApiKey.
            // The free API does not support a requested time period. So we get all the data back and use what we need below. 
            var url = $"https://api.openweathermap.org/data/2.5/forecast?zip={zipcode},us&units={unitParameter}&appid={MyApiKey}";

            try
            {
                // Send the request and wait for the response.
                var response = await _httpClient.GetAsync(url);

                // Throw an exception if the response code is a failure.
                // Note that the API returns 404 if the location is not found, which seems to be the right code.
                // We could convert this to 400 if needed.
                response.EnsureSuccessStatusCode();

                // Get the JSON content.
                var json = await response.Content.ReadAsStringAsync();

                // Get the parsed results.
                using var doc = JsonDocument.Parse(json);

                // Start at the top level.
                var root = doc.RootElement;

                // Get the latitude and longitude for the requested city.
                var locationLat = root.GetProperty("city").GetProperty("coord").GetProperty("lat").GetDouble();
                var locationLon = root.GetProperty("city").GetProperty("coord").GetProperty("lon").GetDouble();

                // The response is a list of forecast data.
                var forecastList = root.GetProperty("list").EnumerateArray();

                double totalTemp = 0;
                int count = 0;
                bool rainPossible = false;

                // The forecast contains 3 hours values; we have 8 entries per day.
                int maxItems = timePeriod * 8;
                int processed = 0;

                // Process each individual forecast, until we have processed enough for the given time period.
                foreach (var item in forecastList)
                {
                    if (processed >= maxItems)
                        break;

                    // Accumulate temperature
                    double temp = item.GetProperty("main").GetProperty("temp").GetDouble();
                    totalTemp += temp;
                    count++;
                    processed++;

                    // Check for the rain property.
                    if (item.TryGetProperty("rain", out var rainProp) && rainProp.ValueKind == JsonValueKind.Object)
                    {
                        rainPossible = true;
                    }
                    else
                    {
                        // Check for each weather value, looking for rain.
                        foreach (var weather in item.GetProperty("weather").EnumerateArray())
                        {
                            // Make main nullable to avoid a warning.
                            string? main = weather.GetProperty("main").GetString()?.ToLower();
                            if (main != null && main.Contains("rain"))
                            {
                                rainPossible = true;
                                break;
                            }
                        }
                    }

                    if (rainPossible)
                        // Exit now, since rain was detected.
                        break;
                }

                // Get the average temperature.
                double averageTemp = count > 0 ? totalTemp / count : 0;

                // Set up the results to be returned.
                var result = new
                {
                    averageTemperature = Math.Round(averageTemp),
                    unit = units == "fahrenheit" ? "F" : "C",
                    lat = locationLat,
                    lon = locationLon,
                    rainPossible
                };

                return Ok(result);
            }

            catch (HttpRequestException ex)
            {
                return StatusCode(503, $"Error retrieving forecast data: {ex.Message}");
            }

            catch (JsonException ex)
            {
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }
        }
    }
}
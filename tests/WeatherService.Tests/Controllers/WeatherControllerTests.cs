using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using WeatherService.Controllers;
using Xunit;

namespace WeatherService.Tests.Controllers
{
    public class WeatherControllerTests
    {
        // Positive test: verify GetCurrentWeather works with good data (with rain) and with additional bogus (to be ignored) data.
        // We could add a test to verify rain outside of the "weather" object.
        [Fact]
        public async Task GetCurrentWeather_ReturnsExpectedResult_WithRain()
        {
            // Arrange test inputs
            var zipcode = "12345";
            var units = "fahrenheit";

            // This is the response that the test will return.
            var jsonResponse = @"
            {
                ""main"": { ""temp"": 72.5 },
                ""bogus"": { ""bogusValue"": 72.5 },
                ""coord"": { ""lat"": 40.71, ""lon"": -74.01 },
                ""rain"": { ""1h"": 0.5 }
            }";

            // Mock HttpMessageHandler to intercept HTTP requests.
            var handlerMock = new Mock<HttpMessageHandler>();

            // SendAsync to simulate a successful HTTP call.
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // Return a fake 200 OK response with the test json content.
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse),
                });

            // Inject the mock handler into the real HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController (with mocked HttpClient) 
            var controller = new WeatherController(httpClient);

            // Call GetCurrentWeather with valid data
            var result = await controller.GetCurrentWeather(zipcode, units);

            // Result must be of type OkObjectResult (HTTP 200)
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Extract data returned. 
            var data = okResult.Value;

            // Cast to dynamic for property access
            dynamic? resultData = okResult.Value;

            // Make sure we got something back.
            Assert.NotNull(data);

            // Serialize the anonymous object to JSON, then parse it back
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Make sure individual values exist.
            Assert.True(root.TryGetProperty("currentTemperature", out var tempProp));
            Assert.True(root.TryGetProperty("unit", out var unitProp));
            Assert.True(root.TryGetProperty("lat", out var latProp));
            Assert.True(root.TryGetProperty("lon", out var lonProp));
            Assert.True(root.TryGetProperty("rainPossibleToday", out var rainProp));

            // Make sure there is rain.
            Assert.Equal(JsonValueKind.True, rainProp.ValueKind);
        }

        // Positive test: verify GetCurrentWeather works with good data (without rain).
        // We could combine this with the previous test to be more efficient.
        [Fact]
        public async Task GetCurrentWeather_ReturnsExpectedResult_NoRain()
        {
            // Arrange test inputs
            var zipcode = "12345";
            var units = "fahrenheit";

            // This is the response that the test will return.
            var jsonResponse = @"
            {
                ""main"": { ""temp"": 72.5 },
                ""coord"": { ""lat"": 40.71, ""lon"": -74.01 }
            }";

            // Mock HttpMessageHandler to intercept HTTP requests.
            var handlerMock = new Mock<HttpMessageHandler>();

            // SendAsync to simulate a successful HTTP call.
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // Return a fake 200 OK response with the test json content.
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse),
                });

            // Inject the mock handler into the real HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController (with mocked HttpClient) 
            var controller = new WeatherController(httpClient);

            // Call GetCurrentWeather with valid data
            var result = await controller.GetCurrentWeather(zipcode, units);

            // Result must be of type OkObjectResult (HTTP 200)
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Extract data returned. 
            var data = okResult.Value;

            // Cast to dynamic for property access
            dynamic? resultData = okResult.Value;

            // Make sure we got something back.
            Assert.NotNull(data);

            // Serialize the anonymous object to JSON, then parse it back
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Make sure individual values exist.
            Assert.True(root.TryGetProperty("currentTemperature", out var tempProp));
            Assert.True(root.TryGetProperty("unit", out var unitProp));
            Assert.True(root.TryGetProperty("lat", out var latProp));
            Assert.True(root.TryGetProperty("lon", out var lonProp));
            Assert.True(root.TryGetProperty("rainPossibleToday", out var rainProp));

            // Make sure no rain.
            Assert.Equal(JsonValueKind.False, rainProp.ValueKind);
        }

        // Negative tests: verify GetCurrentWeather fails with invalid json response data (missing "}").
        // We could add tests with other cases of invalid json.
        [Fact]
        public async Task GetCurrentWeather_InvalidJson_ReturnsBadRequest()
        {
            // Arrange test inputs
            var zipcode = "12345";
            var units = "fahrenheit";

            // This is the response with invalid json (missing "{}") that the test will return.
            var badJsonResponse = @"
            {
                ""main"": { ""temp"": 72.5 }
            ";

            // Mock HttpMessageHandler to intercept HTTP requests.
            var handlerMock = new Mock<HttpMessageHandler>();

            // SendAsync to simulate a successful HTTP call.
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // Return a fake 200 OK response with the test json content.
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(badJsonResponse),
                });

            // Inject the mock handler into the real HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController (with mocked HttpClient) 
            var controller = new WeatherController(httpClient);

            // Call GetCurrentWeather with valid data
            var result = await controller.GetCurrentWeather(zipcode, units);

            // Result must be of type OkObjectResult (HTTP 200)
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid JSON format", badResult.Value!.ToString());
        }

        // Negative tests: verify GetCurrentWeather works with bad data (zipcode and/or units).
        // - Empty zip code and blank units
        // - Blank zip code
        // - Blank units
        // - Null zip code and null units
        // - Null zip code
        // - Null units
        // - Bad units of kelvin
        // - Wrong bumpy case for units of Fahrenheit
        // - Wrong upper case for units of CELSIUS
        [Theory]
        [InlineData("", "")]
        [InlineData("", "celsius")]
        [InlineData("12345", "")]
        [InlineData(null, null)]
        [InlineData(null, "fahrenheit")]
        [InlineData("01701", null)]
        [InlineData("90210", "kelvin")]
        [InlineData("90210", "Fahrenheit")]
        [InlineData("90210", "CELSIUS")]
        public async Task GetCurrentWeather_InvalidInput_ReturnsBadRequest(string zipcode, string units)
        {
            // Mock HttpMessageHandler (not used in this case due to bogus input)
            var handlerMock = new Mock<HttpMessageHandler>();

            // Inject the mock handler into the real HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController (with mocked HttpClient)
            var controller = new WeatherController(httpClient);

            // Call GetCurrentWeather with bogus data
            var result = await controller.GetCurrentWeather(zipcode, units);

            // Result must be of type BadRequestObjectResult (HTTP 400)
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            // Must get back "Invalid input." message
            Assert.Equal("Invalid input.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetCurrentWeather_HttpRequestException_ReturnsServiceUnavailable()
        {
            // Arrange test inputs
            var zipcode = "12345";
            var units = "fahrenheit";

            // Mock HttpMessageHandler to throw HttpRequestException.
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Weather service unreachable"));

            // Inject the mock handler into the HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController with mocked HttpClient
            var controller = new WeatherController(httpClient);

            // Call GetCurrentWeather
            var result = await controller.GetCurrentWeather(zipcode, units);

            // Assert: Expect a 503 ServiceUnavailable result
            var serviceUnavailableResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, serviceUnavailableResult.StatusCode);

            // Assert that the message includes the expected error text
            Assert.Contains("Weather service unreachable", serviceUnavailableResult.Value!.ToString());
        }

        // Positive test: verify GetAverageWeather works with good data (with rain) and with additional bogus (to be ignored) data.
        // We could add a test to verify rain outside of the "weather" object.
        [Fact]
        public async Task GetAverageWeather_ReturnsExpectedResult_WithRain()
        {
            // Arrange test inputs
            var zipcode = "12345";
            var units = "celsius";
            var timePeriod = 2;

            // This is the response that the test will return.
            var jsonResponse = @"
            {
                ""city"": {
                    ""coord"": { ""lat"": 51.51, ""lon"": -0.13 }
                },
                ""list"": [
                    {
                        ""bogus"": { ""bogusValue"": 10.5 },
                        ""main"": { ""temp"": 10.5 },
                        ""weather"": [ { ""main"": ""Clear"" } ]
                    },
                    {
                        ""main"": { ""temp"": 11.5 },
                        ""weather"": [ { ""main"": ""Rain"" } ]
                    }
                ]
            }";

            // Mock HttpMessageHandler to intercept HTTP requests.
            var handlerMock = new Mock<HttpMessageHandler>();

            // SendAsync to simulate a successful HTTP call.
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // Return a fake 200 OK response with the test json content.
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse),
                });

            // Inject the mock handler into the real HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController (with mocked HttpClient)
            var controller = new WeatherController(httpClient);

            // Call GetAverageWeather with valid data
            var result = await controller.GetAverageWeather(zipcode, units, timePeriod);

            // Result must be of type OkObjectResult (HTTP 200)
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Extract data returned. 
            var data = okResult.Value;

            // Make sure we got something back.
            Assert.NotNull(data);

            // Cast to dynamic for property access
            dynamic? resultData = okResult.Value;

            // Make sure we got something back.
            Assert.NotNull(data);

            // Serialize the anonymous object to JSON, then parse it back
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Make sure individual values exist.
            Assert.True(root.TryGetProperty("averageTemperature", out var tempProp));
            Assert.True(root.TryGetProperty("unit", out var unitProp));
            Assert.True(root.TryGetProperty("lat", out var latProp));
            Assert.True(root.TryGetProperty("lon", out var lonProp));
            Assert.True(root.TryGetProperty("rainPossible", out var rainProp));

            // Make sure there is rain.
            Assert.Equal(JsonValueKind.True, rainProp.ValueKind);
        }

        // Positive test: verify GetAverageWeather works with good data (without rain).
        [Fact]
        public async Task GetAverageWeather_ReturnsExpectedResult_NoRain()
        {
            // Arrange test inputs
            var zipcode = "12345";
            var units = "celsius";
            var timePeriod = 2;

            // This is the response that the test will return.
            var jsonResponse = @"
            {
                ""city"": {
                    ""coord"": { ""lat"": 51.51, ""lon"": -0.13 }
                },
                ""list"": [
                    {
                        ""main"": { ""temp"": 10.5 },
                        ""weather"": [ { ""main"": ""Clear"" } ]
                    },
                    {
                        ""main"": { ""temp"": 11.5 },
                        ""weather"": [ { ""main"": ""Clear"" } ]
                    }
                ]
            }";

            // Mock HttpMessageHandler to intercept HTTP requests.
            var handlerMock = new Mock<HttpMessageHandler>();

            // SendAsync to simulate a successful HTTP call.
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // Return a fake 200 OK response with the test json content.
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse),
                });

            // Inject the mock handler into the real HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController (with mocked HttpClient)
            var controller = new WeatherController(httpClient);

            // Call GetAverageWeather with valid data
            var result = await controller.GetAverageWeather(zipcode, units, timePeriod);

            // Result must be of type OkObjectResult (HTTP 200)
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Extract data returned. 
            var data = okResult.Value;

            // Make sure we got something back.
            Assert.NotNull(data);

            // Cast to dynamic for property access
            dynamic? resultData = okResult.Value;

            // Make sure we got something back.
            Assert.NotNull(data);

            // Serialize the anonymous object to JSON, then parse it back
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Make sure individual values exist.
            Assert.True(root.TryGetProperty("averageTemperature", out var tempProp));
            Assert.True(root.TryGetProperty("unit", out var unitProp));
            Assert.True(root.TryGetProperty("lat", out var latProp));
            Assert.True(root.TryGetProperty("lon", out var lonProp));
            Assert.True(root.TryGetProperty("rainPossible", out var rainProp));

            // Make sure there is no rain.
            Assert.Equal(JsonValueKind.False, rainProp.ValueKind);
        }

        // Negative tests: verify GetAverageWeather fails with invalid json response data (missing "}").
        // We could add tests with other cases of invalid json.
        [Fact]
        public async Task GetAverageWeather_InvalidJson_ReturnsBadRequest()
        {
            // Arrange test inputs
            var zipcode = "12345";
            var units = "fahrenheit";
            var timePeriod = 2;

            // This is the response with invalid json (missing "}") that the test will return.
            var badJsonResponse = @"
            {
                ""main"": { ""temp"": 72.5 }
            ";

            // Mock HttpMessageHandler to intercept HTTP requests.
            var handlerMock = new Mock<HttpMessageHandler>();

            // SendAsync to simulate a successful HTTP call.
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // Return a fake 200 OK response with the test json content.
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(badJsonResponse),
                });

            // Inject the mock handler into the real HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController (with mocked HttpClient) 
            var controller = new WeatherController(httpClient);

            // Call GetAverageWeather with valid data
            var result = await controller.GetAverageWeather(zipcode, units, timePeriod);

            // Result must be of type OkObjectResult (HTTP 200)
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid JSON format", badResult.Value!.ToString());
        }

        // Negative tests: verify GetAverageWeather works with bad data (zipcode and/or units).
        // - Empty zip code and units.
        // - Empty zip code.
        // - Empty units.
        // - Null zip code.
        // - Null units.
        // - Bad units of kelvin
        // - Wrong bumpy case for units of Fahrenheit
        // - Wrong upper case for units of CELSIUS
        // - Bad timePeriod: too low
        // - Bad timePeriod: too high
        // We could add tests to check for a forecast with no items.
        [Theory]
        [InlineData("", "", 2)]
        [InlineData("", "celsius", 3)]
        [InlineData("12345", "", 4)]
        [InlineData(null, "fahrenheit", 5)]
        [InlineData("01701", null, 2)]
        [InlineData("90210", "kelvin", 3)]
        [InlineData("90210", "Fahrenheit", 4)]
        [InlineData("90210", "CELSIUS", 5)]
        [InlineData("90210", "celsius", 1)]
        [InlineData("90210", "fahrenheit", 6)]
        public async Task GetAverageWeather_InvalidInput_ReturnsBadRequest(string zipcode, string units, int timePeriod)
        {
            // Mock HttpMessageHandler (not used in this case due to bogus input)
            var handlerMock = new Mock<HttpMessageHandler>();

            // Inject the mock handler into the real HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController (with mocked HttpClient)
            var controller = new WeatherController(httpClient);

            // Call GetAverageWeather with bogus data
            var result = await controller.GetAverageWeather(zipcode, units, timePeriod);

            // Result must be of type BadRequestObjectResult (HTTP 400)
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            // Must get back "Invalid input." message
            Assert.Equal("Invalid input.", badRequestResult.Value);
        }
        
        // Negative test: verify GetAverageWeather fails with HttpRequestException.
        [Fact]
        public async Task GetAverageWeather_HttpRequestException_ReturnsServiceUnavailable()
        {
            // Arrange test inputs
            var zipcode = "12345";
            var units = "fahrenheit";
            var timePeriod = 4;

            // Mock HttpMessageHandler to throw HttpRequestException.
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Weather service unreachable"));

            // Inject the mock handler into the HttpClient.
            var httpClient = new HttpClient(handlerMock.Object);

            // Create an instance of WeatherController with mocked HttpClient
            var controller = new WeatherController(httpClient);

            // Call GetAverageWeather
            var result = await controller.GetAverageWeather(zipcode, units, timePeriod);

            // Assert: Expect a 503 ServiceUnavailable result
            var serviceUnavailableResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, serviceUnavailableResult.StatusCode);

            // Assert that the message includes the expected error text
            Assert.Contains("Weather service unreachable", serviceUnavailableResult.Value!.ToString());
        }
    }
}
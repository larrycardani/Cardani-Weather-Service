# Weather App Project - Larry Cardani

This project includes a weather-related API (built with ASP.NET / C#), a Python CLI tool, unit tests and Docker support.

## Structure

- `webapi/` — ASP.NET Web API
- `tests/` — Unit tests for the API
- `cli/` — Python command-line interface
- `docker/` — Dockerfile, .dockerignore, and prebuilt image

## How to Run on Windows:

Note: HTTPS is supported when running locally. The Docker container serves the API over HTTP (port 80) for simplicity and EC2 compatibility.

### Web API

From the top level folder (control-C when done):
```bash
cd webapi\WeatherService
dotnet run
```
Test from a browser:
1. Verify the Swagger page: https://localhost:7001/Swagger/index.html
2. Verify the first endpoint (Current Weather): https://localhost:7001/Weather/Current/01545?units=fahrenheit
3. Verify the second endpoint (Average Weather): https://localhost:7001/Weather/Average/01545?units=fahrenheit&timePeriod=3

### Unit Tests

From the top level folder:
```bash
cd tests\WeatherService.Tests
dotnet test
```
### Python CLI

From the top level folder:
```bash
cd cli\weathercli
python weathercli.py get-current-weather 12345 celsius
python weathercli.py get-average-weather 12345 fahrenheit 5
```
### Run the prebuilt Docker image on local Windows machine

Start Docker Desktop. Verify no errors.

From the top level folder (control-C when done):
```bash
docker load -i docker/weather-service.tar
docker run --rm -p 7001:80 --name weatherapi weather-service
```
Test from a browser:
1. Verify the Swagger page: http://localhost:7001/Swagger/index.html
2. Verify the first endpoint (Current Weather): http://localhost:7001/Weather/Current/01545?units=fahrenheit
3. Verify the second endpoint (Average Weather): http://localhost:7001/Weather/Average/01545?units=fahrenheit&timePeriod=3

### Build the Docker image yourself:

From the top level folder:
```bash
docker build -f docker/Dockerfile -t weather-service .
```
Create a tar file with the docker image:
```bash
docker save -o docker/weather-service.tar weather-service
```
## How to Run docker image (from tar file) on AWS Linux EC2 instance:

### Assumptions:
- EC2 instance is: Amazon Linux 2 ami 
- ppk key pair allowing putty access
- weather-service.tar has been copied to the EC2 instance to /home/ec2-user

## Access Linux EC2 instance:
Putty to ec2-user@n.n.n.n

### Ensure Docker is installed:
```bash
sudo amazon-linux-extras install docker
```
### Start Docker:
```bash
sudo service docker start
sudo usermod -a -G docker ec2-user
```
Log out and back in to apply Docker group change.

### Load the docker image from the tar file
```bash
docker load -i weather-service.tar
```
### Load the docker image from the tar file
```bash
docker run --rm -p 7001:80 --name weatherapi weather-service
```
## Second access Linux EC2 instance:
ec2-user@n.n.n.n

## Access endpoints via curl:
```bash
curl http://localhost:7001/Weather/Current/01545?units=fahrenheit

curl "http://localhost:7001/Weather/Average/01545?units=fahrenheit&timePeriod=3"
```

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen();

// Register the controllers.
builder.Services.AddControllers();

// Register HttpClient
builder.Services.AddHttpClient();

// Configure URLs based on environment:
if (builder.Environment.IsDevelopment())
{
    // Local dev: listen on HTTPS 7001 (default)
    builder.WebHost.UseUrls("https://localhost:7001");
}
else
{
    // Docker or production: listen on HTTP port 80
    builder.WebHost.UseUrls("http://+:80");
}

var app = builder.Build();

// Map controllers
app.MapControllers();

// Enable Swagger always, for now.
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    // Use HTTPS redirection locally
    app.UseHttpsRedirection();
}
else
{
    // In production / docker, skip HTTPS redirection because only HTTP is enabled
    // No app.UseHttpsRedirection();
}

app.Run();
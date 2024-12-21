using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext configuration (replace "ApplicationDbContext" and "DefaultConnection" with your actual DbContext and connection string name)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient service
builder.Services.AddHttpClient();

// Add controllers and other necessary services
builder.Services.AddControllers();

// Add CORS policy to allow front-end (e.g., Angular) to make requests
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:4200", "https://msi.snovasys.com") // Replace with your front-end URL
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Add Swagger for API documentation (without JWT authentication setup)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Documentation",
        Version = "v1"
    });
});

var app = builder.Build();

// Use routing
app.UseRouting();

// Use CORS policy
app.UseCors(); // Make sure CORS is enabled before any API calls are processed

// No Authorization middleware (as you don't want to use it)
// app.UseAuthorization(); // This is removed based on your requirement

// Enable Swagger UI if in development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger(); // Enable Swagger middleware
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Documentation V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the root (optional)
    });
}

// Map controllers
app.MapControllers();

app.Run();

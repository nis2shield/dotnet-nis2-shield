using Nis2Shield.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🛡️ Register NIS2 Shield
builder.Services.AddNis2Shield(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🛡️ Activate NIS2 Shield Middleware (before Auth)
app.UseNis2Shield();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Minimal API endpoints for testing
app.MapGet("/", () => "NIS2 Shield Demo App - Visit /swagger for API docs");

app.MapGet("/api/health", () => new { status = "healthy", nis2_shield = "active" });

app.MapGet("/api/protected", () => new { message = "This endpoint is protected by NIS2 Shield" });

app.MapPost("/api/data", (DataRequest request) => new { 
    received = request, 
    timestamp = DateTime.UtcNow 
});

app.Run();

// Request model
public record DataRequest(string Name, string Email);

using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var serializeOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

app.MapGet("/laptops/", () => Results.Ok(
        JsonSerializer.Deserialize<List<Hardware>>(
            File.ReadAllText("json/laptops.json"), serializeOptions)))
    .WithName("GetLaptops")
    .WithOpenApi();

app.MapGet("/pcs/", () => Results.Ok(
        JsonSerializer.Deserialize<List<Hardware>>(
            File.ReadAllText("json/pcs.json"), serializeOptions)))
    .WithName("GetPCs")
    .WithOpenApi();

app.Run();


public class Hardware
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public string Processor { get; set; }
    public string Ram { get; set; }
    public string Storage { get; set; }
    public string Gpu { get; set; }
    public double Price { get; set; }
    public string Availability { get; set; }
    public string Description { get; set; }
}
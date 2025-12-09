using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

var serializeOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

app.MapGet("/items/", () => Results.Ok(
        JsonSerializer.Deserialize<List<Hardware>>(
            File.ReadAllText("json/items.json"), serializeOptions)))
    .WithName("GetHardwareItems");

app.Run();


public class Hardware
{
    public int Id { get; set; }
    public string Image { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Processor { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Ram { get; set; } = null!;
    public string Storage { get; set; } = null!;
    public string Gpu { get; set; } = null!;
    public double Price { get; set; }
    public string Availability { get; set; } = null!;
    public string Description { get; set; } = null!;
}
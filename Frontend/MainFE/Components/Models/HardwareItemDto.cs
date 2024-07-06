namespace MainFE.Components.Models;

public class HardwareItemDto
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public string Processor { get; set; }
    public string Ram { get; set; }
    public string Storage { get; set; }
    public string Gpu { get; set; }
    public double Price { get; set; }
    public string Availability { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
    public bool IsDetailsVisible { get; set; }
}
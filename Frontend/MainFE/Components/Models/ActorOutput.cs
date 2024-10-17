namespace MainFE.Components.Models;

public class ActorOutput
{
    public int Id { get; set; }
    public string UserMsg { get; set; }
    public string ActorMsg { get; set; }
    public bool IsExpanded { get; set; } = true;
    public DateTime Time { get; set; }
    public string Model { get; set; }
}
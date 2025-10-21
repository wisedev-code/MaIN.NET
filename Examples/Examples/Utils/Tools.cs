using System.Text.Json;

namespace Examples.Utils;

public static class Tools
{
    public static object GetCurrentTime()
    {
        var now = DateTime.Now;
        var timeInfo = new
        {
            date = now.ToString("yyyy-MM-dd"),
            time = now.ToString("HH:mm:ss"),
            dayOfWeek = now.DayOfWeek.ToString()
        };
        
        return $"Date: {timeInfo.date}, Time: {timeInfo.time}, Day of week: {timeInfo.dayOfWeek}";
    }
}
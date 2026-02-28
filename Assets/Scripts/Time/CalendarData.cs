public static class CalendarData
{
    public static string GetSeasonName(Season season)
    {
        return season switch
        {
            Season.Spring => "Spring",
            Season.Summer => "Summer",
            Season.Autumn => "Autumn",
            Season.Winter => "Winter",
            _ => "Unknown"
        };
    }

    public static string GetDateString(int day, Season season, int year)
    {
        return $"Day {day} — {GetSeasonName(season)}, Year {year}";
    }
}

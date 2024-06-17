namespace MainFE;

public static class Extensions
{
    public static string PrepareMd(this string md) =>
        md.Replace("* ", "*")
            .Replace(" *", "*")
            .Replace("` ", "`")
            .Replace(" `", "`");

}
namespace Whitelabel_backoffice.Models.ViewModels;

public class APIGamesViewModel
{
    public string ApiCode { get; set; } = "ggx";
    public string? ProviderCode { get; set; }
    public string Title { get; set; } = "API Games";

    public List<APIGameProviderOption> Providers { get; set; } = new();
    public List<APIGameColumn> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}

public class APIGameProviderOption
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";
}

public class APIGameColumn
{
    public string Field { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "text";
    public bool Visible { get; set; } = true;
}
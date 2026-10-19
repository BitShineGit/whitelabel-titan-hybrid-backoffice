namespace Whitelabel_backoffice.Models.ViewModels
{
    public class APIProviderTableViewModel
    {
        public string ApiCode { get; set; } = "";
        public string Title { get; set; } = "";
        public List<APIProviderColumn> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public List<APIProviderOption> WLProviderOptions { get; set; } = new();
        public List<string> BulkSettings { get; set; } = new();
    }

    public class APIProviderColumn
    {
        public string Field { get; set; } = "";
        public string Title { get; set; } = "";
        public string Type { get; set; } = "text";
        public bool Visible { get; set; } = true;
        public bool Editable { get; set; }
        public string? Setting { get; set; }
    }

    public class APIProviderOption
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
    }
}

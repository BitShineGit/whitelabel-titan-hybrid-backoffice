namespace Whitelabel_backoffice.Models.ViewModels
{
    public class BettingChartItem
    {
        public string Label { get; set; } = string.Empty;

        public decimal BetAmount { get; set; }

        public decimal WinAmount { get; set; }

        public decimal Ggr { get; set; }
    }
}

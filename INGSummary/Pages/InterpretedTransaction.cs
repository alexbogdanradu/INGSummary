using System.Collections.Generic;

namespace INGSummary.Pages
{
    public class InterpretedTransaction
    {
        public int WeekNo { get; set; }
        public string TotalSpent { get; set; }
        public string TotalSpentPreviousPercent { get; set; }
        public List<List<string>> Payments { get; set; }
    }
}
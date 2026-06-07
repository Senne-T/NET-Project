namespace Restaurant.ViewModels.Enquete
{
    public class EnqueteDashboardViewModel
    {
        public List<EnqueteViewModel> Evaluations { get; set; } = new();
        public int TotalEvaluations { get; set; }
        public double AverageScore { get; set; }
        public int[] ScoreDistribution { get; set; } = new int[6];
    }
}
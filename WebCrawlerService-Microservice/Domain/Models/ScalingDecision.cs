namespace WebCrawlerService.Domain.Models
{
    /// <summary>
    /// Decision result for agent pool scaling
    /// </summary>
    public class ScalingDecision
    {
        public bool ShouldScaleUp { get; set; }
        public bool ShouldScaleDown { get; set; }
        public string Reason { get; set; } = null!;

        public static ScalingDecision ScaleUp(string reason) =>
            new() { ShouldScaleUp = true, Reason = reason };

        public static ScalingDecision ScaleDown(string reason) =>
            new() { ShouldScaleDown = true, Reason = reason };

        public static ScalingDecision NoAction() =>
            new() { Reason = "No scaling action needed" };
    }
}

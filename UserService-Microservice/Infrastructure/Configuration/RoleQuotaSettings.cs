namespace UserService.Infrastructure.Configuration;

public class RoleQuotaSettings
{
    public const string SectionName = "RoleBasedQuotas";

    public int Student { get; set; } = 4;
    public int Lecturer { get; set; } = 20;
    public int Staff { get; set; } = 50;
    public int Admin { get; set; } = int.MaxValue;
    public PaidUserQuotaSettings PaidUser { get; set; } = new();
}

public class PaidUserQuotaSettings
{
    public int Free { get; set; } = 4;
    public int Basic { get; set; } = 100;
    public int Premium { get; set; } = 500;
    public int Enterprise { get; set; } = 2000;
}

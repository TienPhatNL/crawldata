using FluentValidation;

namespace WebCrawlerService.Application.Features.CrawlJob.Commands;

public class StartCrawlJobCommandValidator : AbstractValidator<StartCrawlJobCommand>
{
    public StartCrawlJobCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Urls)
            .NotEmpty()
            .WithMessage("At least one URL is required")
            .Must(urls => urls.Length <= 100)
            .WithMessage("Maximum 100 URLs allowed per job");

        RuleForEach(x => x.Urls)
            .NotEmpty()
            .WithMessage("URL cannot be empty")
            .Must(BeValidUrl)
            .WithMessage("Invalid URL format");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(300)
            .WithMessage("Timeout must be between 1 and 300 seconds");

        RuleFor(x => x.MaxRetries)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(5)
            .WithMessage("Max retries must be between 0 and 5");

        RuleFor(x => x.ConfigurationJson)
            .Must(BeValidJson)
            .WithMessage("Configuration must be valid JSON");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
using System.Data;
using FluentValidation;
using MOE_System.Application.DTOs.TopUp.Request;
using MOE_System.Domain.Enums;

namespace MOE_System.Application.Validators.Topup;

public sealed class CreateScheduledTopUpRequestValidator : AbstractValidator<CreateScheduledTopUpRequest>
{
    public CreateScheduledTopUpRequestValidator()
    {
        RuleFor(x => x.RuleName)
            .NotEmpty().WithMessage("Rule name is required.")
            .MaximumLength(100).WithMessage("Rule name must not exceed 100 characters.");

        RuleFor(x => x.TopupAmount)
            .GreaterThan(0).WithMessage("Top-up amount must be greater than zero.");

        When(x => x.RuleTargetType == RuleTargetType.Individual, () =>
        {
            RuleFor(x => x.TargetEducationAccountId)
                .NotNull().WithMessage("Target Education Account ID is required for Individual top-up type.")
                .Must(x => x != null && x.Any())
                .WithMessage("Target Education Account ID is required for Individual top-up type.")
                .Must(x => x != null && x.All(id => !string.IsNullOrWhiteSpace(id)))
                .WithMessage("Education account ID must not be empty.")
                .Must(x => x != null && x.Distinct().Count() == x.Count)
                .WithMessage("Duplicate education account IDs are not allowed.");
        });

        When(x => x.RuleTargetType == RuleTargetType.Batch, () =>
        {
            RuleFor(x => x.TargetEducationAccountId)
                .Must(x => x == null || !x.Any())
                .WithMessage("Target education account must be empty for batch rule.");
        });

        When(x => x.RuleTargetType == RuleTargetType.Batch, () =>
        {
            RuleFor(x => x.BatchFilterType)
                .NotNull()
                .WithMessage("Invalid batch filter type.")
                .IsInEnum()
                .WithMessage("Invalid batch filter type.");

            When(x => x.BatchFilterType == BatchFilterType.Customized, () =>
            {
                RuleFor(x => x.MinAge)
                    .GreaterThanOrEqualTo(0)
                    .When(x => x.MinAge.HasValue && x.MaxAge.HasValue);

                RuleFor(x => x.MaxAge)
                    .GreaterThanOrEqualTo(0)
                    .When(x => x.MaxAge.HasValue && x.MinAge.HasValue);

                RuleFor(x => x.MinAge)
                    .LessThanOrEqualTo(x => x.MaxAge!.Value)
                    .When(x => x.MinAge.HasValue && x.MaxAge.HasValue)
                    .WithMessage("Minimum age must be less than or equal to maximum age.");

                RuleFor(x => x.MinBalance)
                    .GreaterThanOrEqualTo(0)
                    .When(x => x.MinBalance.HasValue && x.MaxBalance.HasValue);

                RuleFor(x => x.MaxBalance)
                    .GreaterThanOrEqualTo(0)
                    .When(x => x.MaxBalance.HasValue && x.MinBalance.HasValue);

                RuleFor(x => x.MinBalance)
                    .LessThanOrEqualTo(x => x.MaxBalance!.Value)
                    .When(x => x.MinBalance.HasValue && x.MaxBalance.HasValue)
                    .WithMessage("Minimum balance must be less than or equal to maximum balance.");

                RuleFor(x => x)
                    .Must(HasAtLeastOneFilter)
                    .WithMessage("At least one filter must be specified when using customized batch filter type.");
            });
        });

        When(x => !x.ExecuteImmediately, () =>
        {
            RuleFor(x => x.ScheduledTime)
                .NotEmpty().WithMessage("Scheduled time is required for non-immediate execution.")
                .Must(time => time.HasValue && time.Value.Kind == DateTimeKind.Utc)
                .WithMessage("Scheduled time must be in UTC format.")
                .GreaterThan(x => DateTime.UtcNow.AddMinutes(5)) 
                .WithMessage("Scheduled time must be at least 5 minutes in the future.");
        });

        // When ExecuteImmediately is true, ScheduledTime is not required and no validation is applied.
    }

    private bool HasAtLeastOneFilter(CreateScheduledTopUpRequest request)
    {
        return request.MinAge.HasValue ||
               request.MaxAge.HasValue ||
               request.MinBalance.HasValue ||
               request.MaxBalance.HasValue ||
               request.EducationLevelIds is { Count: > 0 } ||
               request.SchoolingStatusIds is { Count: > 0 };
    }
}
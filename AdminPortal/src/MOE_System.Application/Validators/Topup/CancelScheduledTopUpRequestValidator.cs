using FluentValidation;
using MOE_System.Application.DTOs.TopUp.Request;
using MOE_System.Domain.Enums;

namespace MOE_System.Application.Validators.Topup;

public sealed class CancelScheduledTopUpRequestValidator : AbstractValidator<CancelScheduledTopUpRequest>
{
    public CancelScheduledTopUpRequestValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type must be either Batch or Individual.");

        When(x => x.Type == RuleTargetType.Individual, () =>
        {
            RuleFor(x => x.EducationAccountId)
                .NotEmpty()
                .WithMessage("EducationAccountId is required when cancelling individual target.");
        });

        When(x => x.Type == RuleTargetType.Batch, () =>
        {
            RuleFor(x => x.EducationAccountId)
                .Empty()
                .WithMessage("EducationAccountId should not be provided when cancelling batch rule.");
        });
    }
}

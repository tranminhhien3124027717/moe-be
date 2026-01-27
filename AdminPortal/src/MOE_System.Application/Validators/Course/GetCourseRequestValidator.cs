using FluentValidation;
using MOE_System.Application.DTOs.Course.Request;

namespace MOE_System.Application.Validators.Course;

public sealed class GetCourseRequestValidator : AbstractValidator<GetCourseRequest>
{
    public GetCourseRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("Search term cannot exceed 200 characters.");

        RuleFor(x => x.TotalFeeMin)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalFeeMin.HasValue)
            .WithMessage("Total fee minimum must be at least 0.");
        
        RuleFor(x => x.TotalFeeMax)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalFeeMax.HasValue)
            .WithMessage("Total fee maximum must be at least 0.");

        RuleFor(x => x.TotalFeeMax)
            .GreaterThanOrEqualTo(x => x.TotalFeeMin!.Value)
            .When(x => x.TotalFeeMin.HasValue && x.TotalFeeMax.HasValue)
            .WithMessage("Total fee maximum must be greater than or equal to total fee minimum.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be greater than or equal to start date.");

        ValidateNullableCollection(RuleFor(x => x.Provider), "Provider");
        ValidateNullableCollection(RuleFor(x => x.ModeOfTraining), "Mode of Training");
        ValidateNullableCollection(RuleFor(x => x.Status), "Status");
        ValidateNullableCollection(RuleFor(x => x.PaymentType), "Payment Type");
        ValidateNullableCollection(RuleFor(x => x.BillingCycle), "Billing Cycle");

        RuleFor(x => x.SortBy)
            .IsInEnum()
            .WithMessage("Sort by contains an invalid value.");

        RuleFor(x => x.SortDirection)
            .IsInEnum()
            .WithMessage("Sort direction contains an invalid value.");
    }

    private void ValidateNullableCollection(IRuleBuilderInitial<GetCourseRequest, List<string>?> ruleBuilder, string fieldName)
    {
        ruleBuilder.Custom((list, context) =>
        {
            if (list == null) return;

            if (list.Count > 20)
            {
                context.AddFailure($"{fieldName} filter exceeds the maximum limit of 50 items.");
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(list[i]))
                {
                    context.AddFailure($"{fieldName}[{i}]", $"The value at index {i} in {fieldName} cannot be empty or whitespace.");
                }
            }
        });
    }
}
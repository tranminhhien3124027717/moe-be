using System;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.TestHelper;
using MOE_System.Application.DTOs.TopUp.Request;
using MOE_System.Application.Validators.Topup;
using MOE_System.Domain.Enums;
using Xunit;

namespace MOE_System.Application.Tests.ValidatorTests;

public class CreateScheduledTopUpRequestValidatorTests
{
    private readonly CreateScheduledTopUpRequestValidator _validator;
    private readonly DateTime _futureTime = DateTime.UtcNow.AddHours(2);

    public CreateScheduledTopUpRequestValidatorTests()
    {
        _validator = new CreateScheduledTopUpRequestValidator();
    }

    #region RuleName Validation Tests

    [Fact]
    public void Validate_WithEmptyRuleName_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: string.Empty,
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleName)
            .WithErrorMessage("Rule name is required.");
    }

    [Fact]
    public void Validate_WithRuleNameExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: new string('a', 101),
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleName)
            .WithErrorMessage("Rule name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithValidRuleName_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Valid Rule Name",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RuleName);
    }

    #endregion

    #region TopupAmount Validation Tests

    [Fact]
    public void Validate_WithZeroTopupAmount_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 0m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TopupAmount)
            .WithErrorMessage("Top-up amount must be greater than zero.");
    }

    [Fact]
    public void Validate_WithNegativeTopupAmount_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: -100m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TopupAmount)
            .WithErrorMessage("Top-up amount must be greater than zero.");
    }

    [Fact]
    public void Validate_WithValidTopupAmount_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TopupAmount);
    }

    #endregion

    #region Individual RuleTargetType Validation Tests

    [Fact]
    public void Validate_IndividualTypeWithoutTargetAccountId_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Individual,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            TargetEducationAccountId: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetEducationAccountId)
            .WithErrorMessage("Target Education Account ID is required for Individual top-up type.");
    }

    [Fact]
    public void Validate_IndividualTypeWithTargetAccountId_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Individual,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            TargetEducationAccountId: new List<string> { "account-123" }
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TargetEducationAccountId);
    }

    #endregion

    #region Batch Type Filter Validation Tests

    [Fact]
    public void Validate_BatchTypeWithInvalidBatchFilterType_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: (BatchFilterType)999,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BatchFilterType)
            .WithErrorMessage("Invalid batch filter type.");
    }

    [Fact]
    public void Validate_BatchTypeWithAllFilter_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BatchFilterType);
    }

    #endregion

    #region Customized Batch Filter Tests

    [Fact]
    public void Validate_CustomizedFilterWithNoFilters_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("At least one filter must be specified when using customized batch filter type.");
    }

    [Fact]
    public void Validate_CustomizedFilterWithMinAgeOnly_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            MinAge: 18
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Validate_CustomizedFilterWithMinAgeGreaterThanMaxAge_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            MinAge: 30,
            MaxAge: 20
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinAge)
            .WithErrorMessage("Minimum age must be less than or equal to maximum age.");
    }

    [Fact]
    public void Validate_CustomizedFilterWithNegativeMinAge_ShouldFail()
    {
        // Arrange - validator only validates negative ages when BOTH MinAge and MaxAge are set
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: false,
            MinAge: -5,
            MaxAge: 30
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Any(e => e.PropertyName == "MinAge").Should().BeTrue();
    }

    [Fact]
    public void Validate_CustomizedFilterWithMinBalanceOnly_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            MinBalance: 100m
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Validate_CustomizedFilterWithMinBalanceGreaterThanMaxBalance_ShouldFail()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            MinBalance: 500m,
            MaxBalance: 100m
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinBalance)
            .WithErrorMessage("Minimum balance must be less than or equal to maximum balance.");
    }

    [Fact]
    public void Validate_CustomizedFilterWithNegativeMinBalance_ShouldFail()
    {
        // Arrange - validator only validates negative balance when BOTH MinBalance and MaxBalance are set
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: false,
            MinBalance: -50m,
            MaxBalance: 5000m
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Any(e => e.PropertyName == "MinBalance").Should().BeTrue();
    }

    [Fact]
    public void Validate_CustomizedFilterWithEducationLevel_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            EducationLevel: EducationLevel.Primary
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Validate_CustomizedFilterWithResidentialStatus_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Validate_CustomizedFilterWithSchoolingStatus_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            SchoolingStatus: SchoolingStatus.InSchool
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Validate_CustomizedFilterWithMultipleFilters_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Customized,
            ScheduledTime: _futureTime,
            ExecuteImmediately: true,
            MinAge: 18,
            MaxAge: 65,
            MinBalance: 100m,
            MaxBalance: 5000m,
            EducationLevel: EducationLevel.Secondary,
            SchoolingStatus: SchoolingStatus.InSchool
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    #endregion

    #region ScheduledTime Validation Tests

    [Fact]
    public void Validate_WithPastScheduledTime_ShouldFail()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var pastTime = now.AddHours(-1);
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: pastTime,
            ExecuteImmediately: false
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Validate_WithFutureScheduledTime_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: false
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledTime);
    }

    [Fact]
    public void Validate_WithExecuteImmediatelyTrue_ScheduledTimeNotRequired()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Test Rule",
            TopupAmount: 1000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: DateTime.UtcNow.AddHours(-5),
            ExecuteImmediately: true
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledTime);
    }

    #endregion

    #region Full Request Validation Tests

    [Fact]
    public void Validate_WithCompleteValidRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Complete Test Rule",
            TopupAmount: 5000m,
            RuleTargetType: RuleTargetType.Batch,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: false,
            MinAge: 18
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCompleteIndividualRequest_ShouldPass()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: "Individual Test Rule",
            TopupAmount: 2000m,
            RuleTargetType: RuleTargetType.Individual,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: _futureTime,
            ExecuteImmediately: false,
            TargetEducationAccountId: new List<string> { "account-id-123" }
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMultipleValidationErrors_ShouldFailAll()
    {
        // Arrange
        var request = new CreateScheduledTopUpRequest(
            RuleName: string.Empty,
            TopupAmount: 0m,
            RuleTargetType: RuleTargetType.Individual,
            BatchFilterType: BatchFilterType.Everyone,
            ScheduledTime: DateTime.UtcNow.AddHours(-1),
            ExecuteImmediately: false,
            TargetEducationAccountId: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion
}

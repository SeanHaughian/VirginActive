using FluentValidation;
using RockTracker.Api.Models;
using RockTracker.Api.Resources;

namespace RockTracker.Api.Validation;

public sealed class CreateRockRequestValidator : AbstractValidator<CreateRockValidationContext>
{
    private static readonly string[] AllowedCategories =
    {
        RockCategory.Revenue.Name,
        RockCategory.Health.Name,
        RockCategory.Career.Name,
        RockCategory.Other.Name
    };

    public CreateRockRequestValidator()
    {
        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage(Messages.MemberIdRequired);

        RuleFor(x => x.Request.Category)
            .NotEmpty().WithMessage(Messages.CategoryRequired)
            .Must(category => AllowedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
                .WithMessage(Messages.CategoryMustBeAllowed)
                .When(x => !string.IsNullOrWhiteSpace(x.Request.Category), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Request.DueDate)
            .NotNull().WithMessage(Messages.DueDateRequired)
            .Must((context, dueDate) => dueDate!.Value > context.Now)
                .WithMessage(Messages.DueDateMustBeFuture)
                .When(x => x.Request.DueDate.HasValue, ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Request.Title)
            .NotEmpty().WithMessage(Messages.TitleRequired);

        RuleFor(x => x)
            .Custom((context, validationContext) => ValidateCategoryRules(context, validationContext));
    }

    private static void ValidateCategoryRules(CreateRockValidationContext context, ValidationContext<CreateRockValidationContext> validationContext)
    {
        var request = context.Request;

        if (string.IsNullOrWhiteSpace(context.MemberId) ||
            string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Category) ||
            !AllowedCategories.Contains(request.Category, StringComparer.OrdinalIgnoreCase) ||
            !request.DueDate.HasValue)
        {
            return;
        }

        var category = RockCategory.FromName(request.Category);
        var tempRock = new Rock
        {
            Id = Guid.Empty,
            MemberId = context.MemberId,
            Title = request.Title,
            Category = category,
            DueDate = request.DueDate.Value,
            Status = RockStatus.Pending,
            Note = request.Note
        };

        if (category.IsValid(tempRock, context.Now))
            return;

        var message = category == RockCategory.Revenue
            ? Messages.RevenueDueDateRule
            : category == RockCategory.Health
                ? Messages.HealthTitleRule
                : category == RockCategory.Career
                    ? Messages.CareerNoteRule
                    : Messages.CategoryValidationFailed;

        validationContext.AddFailure(message);
    }
}

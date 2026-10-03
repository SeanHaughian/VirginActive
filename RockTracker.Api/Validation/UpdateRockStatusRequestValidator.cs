using FluentValidation;
using RockTracker.Api.Models;
using RockTracker.Api.Resources;

namespace RockTracker.Api.Validation;

public sealed class UpdateRockStatusRequestValidator : AbstractValidator<UpdateRockStatusRequest>
{
    public UpdateRockStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotNull().WithMessage(Messages.StatusRequired);
    }
}

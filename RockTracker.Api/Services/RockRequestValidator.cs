using RockTracker.Api.Models;
using RockTracker.Api.Validation;
using System.Linq;

namespace RockTracker.Api.Services;

public class RockRequestValidator : IRockRequestValidator
{
    private readonly FluentValidation.IValidator<CreateRockValidationContext> _createValidator = new CreateRockRequestValidator();
    private readonly FluentValidation.IValidator<UpdateRockStatusRequest> _updateValidator = new UpdateRockStatusRequestValidator();

    public IList<string> ValidateCreate(string memberId, CreateRockRequest? request, IClock clock)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request body is required.");
            return errors;
        }

        var context = new CreateRockValidationContext(memberId, request, clock?.Now ?? DateTimeOffset.UtcNow);
        var result = _createValidator.Validate(context);
        errors.AddRange(result.Errors.Select(validationFailure => validationFailure.ErrorMessage));

        return errors;
    }

    public IList<string> ValidateUpdateStatus(string memberId, Guid rockId, UpdateRockStatusRequest? request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request body is required.");
            return errors;
        }

        var result = _updateValidator.Validate(request);
        errors.AddRange(result.Errors.Select(validationFailure => validationFailure.ErrorMessage));

        return errors;
    }

    public IList<string> ValidateTransition(Rock? existing, RockStatus newStatus)
    {
        var errors = new List<string>();

        if (existing == null)
        {
            errors.Add("Rock not found");
            return errors;
        }

        if (existing.Status != RockStatus.Pending)
        {
            errors.Add($"Invalid transition: rock is in status '{existing.Status}' and cannot be changed.");
            return errors;
        }

        if (newStatus != RockStatus.Completed && newStatus != RockStatus.Missed)
            errors.Add("Invalid target status. Allowed transitions: pending -> completed, pending -> missed");

        return errors;
    }
}

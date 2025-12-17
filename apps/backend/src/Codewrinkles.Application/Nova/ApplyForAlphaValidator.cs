using Kommand;

namespace Codewrinkles.Application.Nova;

public sealed class ApplyForAlphaValidator : IValidator<ApplyForAlphaCommand>
{
    private List<ValidationError> _errors = null!;

    public Task<ValidationResult> ValidateAsync(
        ApplyForAlphaCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        ValidateEmail(command.Email);
        ValidateName(command.Name);
        ValidatePrimaryTechStack(command.PrimaryTechStack);
        ValidateYearsOfExperience(command.YearsOfExperience);
        ValidateGoal(command.Goal);

        return Task.FromResult(_errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success());
    }

    private void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.Email), "Email is required"));
            return;
        }

        if (email.Length > 256)
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.Email), "Email must be 256 characters or less"));
            return;
        }

        // Basic email format validation
        if (!email.Contains('@') || !email.Contains('.'))
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.Email), "Please enter a valid email address"));
        }
    }

    private void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.Name), "Name is required"));
            return;
        }

        if (name.Length > 100)
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.Name), "Name must be 100 characters or less"));
        }
    }

    private void ValidatePrimaryTechStack(string techStack)
    {
        if (string.IsNullOrWhiteSpace(techStack))
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.PrimaryTechStack), "Primary tech stack is required"));
            return;
        }

        if (techStack.Length > 200)
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.PrimaryTechStack), "Primary tech stack must be 200 characters or less"));
        }
    }

    private void ValidateYearsOfExperience(int years)
    {
        if (years < 0)
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.YearsOfExperience), "Years of experience cannot be negative"));
        }

        if (years > 50)
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.YearsOfExperience), "Years of experience seems too high"));
        }
    }

    private void ValidateGoal(string goal)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.Goal), "Please tell us your learning goals"));
            return;
        }

        if (goal.Length < 20)
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.Goal), "Please provide more details about your goals (at least 20 characters)"));
            return;
        }

        if (goal.Length > 2000)
        {
            _errors.Add(new ValidationError(nameof(ApplyForAlphaCommand.Goal), "Goal must be 2000 characters or less"));
        }
    }
}

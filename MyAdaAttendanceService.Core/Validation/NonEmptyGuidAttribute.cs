using System.ComponentModel.DataAnnotations;

namespace MyAdaAttendanceService.Core.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NonEmptyGuidAttribute : ValidationAttribute
{
    public NonEmptyGuidAttribute()
        : base("The {0} field must not be empty.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is Guid guid)
            return guid != Guid.Empty;

        return false;
    }
}

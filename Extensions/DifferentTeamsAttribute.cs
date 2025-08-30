using System.ComponentModel.DataAnnotations;

public class DifferentTeamsAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var match = (MatchViewModel)validationContext.ObjectInstance;

        if (match.HomeTeamID == match.AwayTeamID)
        {
            return new ValidationResult("Ev sahibi ve deplasman takımı aynı olamaz.");
        }

        return ValidationResult.Success;
    }
}

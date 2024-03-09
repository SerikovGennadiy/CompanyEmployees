using Application.Companies.Commands;
using FluentValidation;
using FluentValidation.Results;

namespace Application.Validators
{
    // Опредилить валидатор AbstractValidator c правилами для CreateCompanyCommand
    // c => c.Company -> поле типа CreateCompanyCommandDTO.cs
    public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
    {
        public CreateCompanyCommandValidator()
        {
            RuleFor(c => c.Company.Name).NotEmpty().MaximumLength(60);
            RuleFor(c => c.Company.Address).NotEmpty().MaximumLength(60);
        }

        // нам нужно прогнать через валидатор case когда валидируемый объект не получен -> пришел NULL
        public override ValidationResult Validate(ValidationContext<CreateCompanyCommand> context)
        {
            return context.InstanceToValidate.Company is null ?
                new ValidationResult(new[]
                {
                    new ValidationFailure("CompanyForCreationDto", "CompanyCreateDTO object is null") })
                :
                base.Validate(context);
        }
    }
}

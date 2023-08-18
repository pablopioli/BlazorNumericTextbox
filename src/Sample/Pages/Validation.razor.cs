using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;

namespace Sample.Pages
{
    public partial class Validation
    {
        readonly InputModel Model = new();
        string ValidationResult = "Still not validated";

        void FormSubmitted(EditContext editContext)
        {
            var formIsValid = editContext.Validate();
            ValidationResult = formIsValid ? "Valid" : "Invalid";
        }

        public class InputModel
        {
            public int Age { get; set; }
            public string Name { get; set; }
        }

        public class ModelValidator : AbstractValidator<InputModel>
        {
            public ModelValidator()
            {
                RuleFor(x => x.Age).NotEmpty().WithMessage("Age is required");
                RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            }
        }
    }
}

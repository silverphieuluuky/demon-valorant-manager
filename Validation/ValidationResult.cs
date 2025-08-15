using System.Collections.Generic;

namespace RiotAutoLogin.Validation
{
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<ValidationError> Errors { get; } = new List<ValidationError>();

        public void AddError(string propertyName, string errorMessage)
        {
            Errors.Add(new ValidationError(propertyName, errorMessage));
        }

        public void AddError(ValidationError error)
        {
            Errors.Add(error);
        }

        public void Merge(ValidationResult other)
        {
            if (other != null)
            {
                Errors.AddRange(other.Errors);
            }
        }

        public string GetErrorMessage(string propertyName)
        {
            var error = Errors.Find(e => e.PropertyName == propertyName);
            return error?.ErrorMessage ?? string.Empty;
        }

        public bool HasError(string propertyName)
        {
            return Errors.Exists(e => e.PropertyName == propertyName);
        }
    }

    public class ValidationError
    {
        public string PropertyName { get; }
        public string ErrorMessage { get; }

        public ValidationError(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }
} 
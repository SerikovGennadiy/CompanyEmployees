using Entities.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Validations
{
    public class ScienceBookAttribute : ValidationAttribute
    {
        public BookGenre Genre { get; set; }
        public string Error => $"The genre of the book must be {BookGenre.Science}";

        public ScienceBookAttribute(BookGenre genre)
        {
            Genre = genre;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var book = (Book)validationContext.ObjectInstance;

            if (!book.Genre.Equals(Genre.ToString()))
                return new ValidationResult(Error);

            return ValidationResult.Success;
        }
    }

    public enum BookGenre
    {
        Science,
        Poetry,
        Biografy,
        Roman
    }
}

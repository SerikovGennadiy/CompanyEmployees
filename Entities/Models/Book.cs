using Entities.Validations;
using System.ComponentModel.DataAnnotations;

namespace Entities.Models
{
    public class Book : IValidatableObject
    {
        public int Id { get; set; }
        
        [Required]
        public string? Title { get; set; }
        
        [Range(10, int.MaxValue)]
        public int Price { get; set;}

       /* [ScienceBook(BookGenre.Science)]*/
        public string? Genre { get; set; }

        // with this method we don't apply [ScienceBook] attribute
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errorMessage = $"The genre of book must be {BookGenre.Science}";
            if (!Genre.Equals(BookGenre.Science.ToString()))
                yield return new ValidationResult(errorMessage, new[] { nameof(Genre) });
        }
    }
}


using Entities.Exceptions;
using FluentValidation;
using MediatR;

namespace Application.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        // IValidator validator implementations collection
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        // implementation IPipelineBehavior

        // FluentValidation сканирует проект объекты AbstractValidation (см Ap.Validator..). Анализирует правила 
        // проверки указанных классов и предоставляет экземпляр при обработке запроса от клиента.

        // Если ошибки есть кинет всех их в исключение
        // Exception перехватится в GLOBAL ERROR HANDLER
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // если валидаторов нет - кидаем request дальше
            if (!_validators.Any())
                return await next();

            // если есть загребаем request и чз Validate прогоняем его чз FluentValidation.AbstractValidator.
            // В запросе кинут CreateCompanyCommandDTO, у нас есть реализация поэтому обрабатываем
            var context = new ValidationContext<TRequest>(request);

            // собираем косяки
            var errorsDictionary = _validators
                .Select(x => x.Validate(context))
                .SelectMany(x => x.Errors)
                .Where(x => x != null)
                .GroupBy(
                    x => x.PropertyName.Substring(x.PropertyName.IndexOf('.') + 1),
                    x => x.ErrorMessage,
                    (propertyName, errorMessages) => new
                    {
                        Key = propertyName,
                        Values = errorMessages.Distinct().ToArray()
                    })
                .ToDictionary(x => x.Key, x => x.Values);

                // кидаем Exception если косяки есть
                if (errorsDictionary.Any())
                    throw new ValidationAppException(errorsDictionary);

            // кидаем запрос дальше если нет
            return await next();
        }
    }
}

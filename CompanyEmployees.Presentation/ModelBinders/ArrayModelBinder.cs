using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel;
using System.Reflection;

namespace CompanyEmployees.Presentation.ModelBinders
{
    /*
        model binder for IEnumerable type
        1. check if out parametr is the same type
        2. extract the our value (comma-separated Guid's string) with ValueProvider.GetValue()
        2.1 baceuse it's parametr string - check for null or empty
            if null - return null. (we already have check for null in controller!)
        3. genericType store type which specify in CompanyController and T argument (IEnumerable<Guid>..)
        4. converter - converter for GUID type. 
            BUT WE DIDN'T FORCE THE GUID TYPE in this model. 
            INSTEAD WE INSPECTED WHAT IS THE NESTED TYPE of the IEnumerable parameter
            AND CREATED PROPER CONVERTER FOR EXACT TYPE (THUS MAKING THIS BINDER GENERIC!)
        5. objectArray - consist Guid string array we send to the API
        6. guidArray - future returned array of GUID type converted, copied here.
        7. bindingContext - assign itself guidArray 
            to DO CORRECT BINDING /(guid:string,guid:string,guid:string) -> IEnumerable<Guid>...)
        8. don't forget mark binded param in controller
            public IActionResult 
                GetCompanyCollection([ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids) {..
     */ 
    public class ArrayModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if(!bindingContext.ModelMetadata.IsEnumerableType)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            var providedValue = bindingContext.ValueProvider
                .GetValue(bindingContext.ModelName)
                .ToString();

            if (string.IsNullOrEmpty(providedValue))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            var genericType = 
                bindingContext.ModelType.GetTypeInfo().GenericTypeArguments[0];

            var converter = TypeDescriptor.GetConverter(genericType);

            var objectArray = providedValue.Split(new[] { "," },
                StringSplitOptions.RemoveEmptyEntries)
                .Select(x => converter.ConvertFromString(x.Trim()))
                .ToArray();

            var guidArray = Array.CreateInstance(genericType, objectArray.Length);
            objectArray.CopyTo(guidArray, 0);
            bindingContext.Model = guidArray;

            bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
            return Task.CompletedTask;
        }
    }
}

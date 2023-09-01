using Contracts;
using Entities.Models;
using System.Reflection;

namespace Service.DataShaping
{
    // DINAMICALLY ADD JUST THE PROPERTIES WE NEED TO OUR DINAMIC OBJECT.
    // for data shapping collection of entities
    /*  Types:
     * ExpandoObject(Data Shaping)
     * Entity(XML DataShaping) 
     * ShapedEntity(HATEOUS)
     */
    public class DataShaper<T> : IDataShaper<T> where T : class
    {
        // properties we're going to pull out (извлечь из вводимого типа T (Company or Employee)
        public PropertyInfo[] Properties { get; set; }

        public DataShaper()
        {
            // get properties of input class
            Properties = typeof(T)
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        public IEnumerable<ShapedEntity> ShapeData (IEnumerable<T> entities, string fieldString)
        {
            var requiredProperties = GetRequiredProperties(fieldString);
            return FetchData(entities, requiredProperties);
        }

        public ShapedEntity ShapeData(T entity, string fieldString)
        {
            var requiredProperties = GetRequiredProperties(fieldString);
            return FetchDataForEntity(entity, requiredProperties);
        }

        // parsing input string that contains the fields we want to fetch
        // to hust return properties we need to retrun to the controller
        private IEnumerable<PropertyInfo> GetRequiredProperties(string fieldString)
        {
            var requiredProperties = new List<PropertyInfo>();
            // check for empty
            if(!string.IsNullOrEmpty(fieldString))
            {
                // to pull out every required fields from fieldString
                var fields = fieldString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in fields)
                {
                    // find just T class properties that match with fileds from fieldString
                    var property = Properties.FirstOrDefault(pi =>
                    pi.Name.Equals(item.Trim(), StringComparison.InvariantCultureIgnoreCase));

                    if (property is null)
                        continue;

                    requiredProperties.Add(property);
                }
            }
            else
            {
                // or return all properties by default
                requiredProperties = Properties.ToList();
            }

            return requiredProperties;
        }

        // extract values for our properties utilizing FetchDataForEntity
        private IEnumerable<ShapedEntity> FetchData(IEnumerable<T> entities, IEnumerable<PropertyInfo> requiredProperties)
        {
            var shapedData = new List<ShapedEntity>();
            foreach (var entity in entities)
            {
                var shapedObject = FetchDataForEntity(entity, requiredProperties);
                shapedData.Add(shapedObject);
            }
            return shapedData;
        }

        // extract values for this required properties we prepared
        // ExpandoObject implements IDictionary<string, object> we can use TryAdd method
        // to add our property using its name as a key and the values as a value for the dictionarry
        private ShapedEntity FetchDataForEntity(T entity, IEnumerable<PropertyInfo> requiredProperties)
        {
            var shapedObject = new ShapedEntity();
            var shapedObjectType = shapedObject.GetType();
            foreach (var property in requiredProperties)
            {
                var objectPropertyValue = property.GetValue(entity);
                //shapedObject.TryAdd(property.Name, objectPropertyValue); // its for ExpandoObject
                // property.SetValue(entity, objectValueProperty);                
                shapedObject.Entity.TryAdd(property.Name, objectPropertyValue);
            }

            var objectProperty = entity.GetType().GetProperty("Id");
            shapedObject.Id = (Guid)objectProperty.GetValue(entity);

            return shapedObject;
        }
    }
}

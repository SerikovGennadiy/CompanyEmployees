using Entities;
using Entities.Models;
using System.Dynamic;

namespace Contracts
{
    public interface IDataShaper<T>
    {
        // for data shapping collection of entities
                /*  Types:
                 * ExpandoObject(Data Shaping)
                 * Entity(XML DataShaping) 
                 * ShapedEntity(HATEOUS)
                 */
        IEnumerable<ShapedEntity> ShapeData(IEnumerable<T> entities, string fieldString);
        // for data shaping single entity
        ShapedEntity ShapeData(T entity, string fieldString);
    }
}

// System.Dynamic.ExpandObject (type) allows using object like in JS
/*
    var person = new ExpandObject();
    person.Age = 10;
    person.Name = "Miki";

    person.UpAge = (Action<int>)(x => person.Age += x );
    person.Upage(4);
    ......
 */

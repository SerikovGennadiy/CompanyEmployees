using Entities.LinkModels;
using System.Xml;
using System.Dynamic;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace Entities
{
    public class Entity : DynamicObject, IXmlSerializable, IDictionary<string, object?>
    {
       IDictionary<string, object?>_items = new
            Dictionary<string, object?>();

        public object? this[string key] {
            get =>  _items[key];
            set => _items[key] = value;
        }

        public ICollection<string> Keys =>
            _items.Select(x => x.Key).ToList();

        public ICollection<object?> Values =>
            _items.Select(x => x.Value).ToList();

        public int Count => _items.Count();

        public bool IsReadOnly =>
           typeof(Entity)
                .GetMember("_items")
                    .IsReadOnly;
        public void Add(string key, object? value) =>
            _items.Add(new KeyValuePair<string, object?>(key, value));

        public void Add(KeyValuePair<string, object?> item) => 
            _items.Add(item);

        public void Clear() =>
            _items.Clear();
        public bool Contains(KeyValuePair<string, object?> item) =>
            _items.Contains(item);


        public bool ContainsKey(string key) =>
            Keys.Contains(key);
        

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
            _items.CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
            _items.GetEnumerator();
        
        public XmlSchema? GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key) =>
            _items.Remove(key);

        public bool Remove(KeyValuePair<string, object?> item) =>
            _items.Remove(item);
        
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) =>
            _items.TryGetValue(key, out value);

        public void WriteXml(XmlWriter writer)
        {
            foreach (var key in Keys)
            {
                var value = _items[key];
                WriteLinksToXml(key, value, writer);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        private void WriteLinksToXml(string key, object? value, XmlWriter writer)
        {   
            writer.WriteStartElement(key);

            // recursivly call WriteLinksToXml
            if (value is not null && value.GetType() == typeof(List<Link>))
            {
                foreach (var val in (List<Link>)value)
                {
                    writer.WriteStartElement(nameof(Link));
                    WriteLinksToXml(nameof(val.Href), val.Href, writer);
                    WriteLinksToXml(nameof(val.Method), val.Method, writer);
                    WriteLinksToXml(nameof(val.Rel), val.Rel, writer);
                    writer.WriteEndElement();
                }
            }
            else
            {
                writer.WriteString(value?.ToString());
            }

            writer.WriteEndElement();
        }
    }
}

using System;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Text.Json;
using System.IO.Compression;
namespace CSharpAbilities
{
#nullable disable
    public interface IPrinter<in T>
    {
        ValueTask PrintAsync(T item);
        void Print(T item);
    }
    public interface ILocalizer<T>
    {
        IDictionary<string, IDictionary<string, T>> Data { get; }
        void AddOrSetResource(string key, T value, CultureInfo info);
        ValueTask<bool> TryLoadData(string from);
        string this[string key] { get; }
    }
    public interface ILocalizationLoader
    {
        bool FillData(string from, IDictionary<string, string> dictionary);
    }
    public struct TextLocalizationLoader : ILocalizationLoader
    {
        public bool FillData(string from, IDictionary<string, string> dictionary)
        {
            string data = null;
            using (StreamReader reader = File.OpenText(from))
            {
                data = reader.ReadToEnd();
            }
            string[] args = data.Split('=', ';');
            string lastKey = null;
            for (int x = 0; x < args.Length; x++)
            {
                if (x == 0 || x % 2 == 0)
                {
                    lastKey = args[x];
                }
                else
                {
                    if (!dictionary.TryAdd(lastKey, args[x]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
    public struct DatLocalizationLoader : ILocalizationLoader
    {
        public bool FillData(string from, IDictionary<string, string> dictionary)
        {
            byte[] buffer = null;
            using (FileStream stream = File.OpenRead(from))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    buffer = new byte[stream.Length];
                    reader.Read(buffer);
                }
            }
            IEnumerable<string> keys = buffer.Select((current) =>
            {
                return string.Concat(current.GetHashCode(), string.Concat(Task.CurrentId, Environment.CurrentManagedThreadId));
            });
            IEnumerable<KeyValuePair<string, string>> parsed = keys.Zip(buffer, (key, value) => new KeyValuePair<string, string>(key, value.ToString()));
            if (!parsed.All(pair => dictionary.TryAdd(pair.Key, pair.Value)))
            {
                return false;
            }
            return true;
        }
    }
    public struct JsonLocalizationLoader : ILocalizationLoader
    {
        public bool FillData(string from, IDictionary<string, string> dictionary)
        {
            SerializableEntity jsonEntity;
            using (FileStream reader = File.OpenRead(from))
            {
                jsonEntity = JsonSerializer.Deserialize<SerializableEntity>(reader);
            }
            foreach (var pair in jsonEntity.Data)
            {
                if (!dictionary.TryAdd(pair.Key, pair.Value))
                {
                    return false;
                }
            }
            return true;
        }
    }
    public struct XmlLocalizationLoader : ILocalizationLoader
    {
        public bool FillData(string from, IDictionary<string, string> dictionary)
        {
            SerializableEntity xmlEntity;
            using (FileStream stream = File.OpenRead(from))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SerializableEntity));
                xmlEntity = (SerializableEntity)serializer.Deserialize(stream);
            }
            foreach (var pair in xmlEntity.Data)
            {
                if (!dictionary.TryAdd(pair.Key, pair.Value))
                {
                    return false;
                }
            }
            return true;
        }
    }
    public struct LocalLocalizer : ILocalizer<string>
    {
        public IDictionary<string, IDictionary<string, string>> Data { get; private set; }
        private Dictionary<string, ILocalizationLoader> _loaders { get; }
        private IPrinter<string> _printer { get; }
        public LocalLocalizer(IDictionary<string, string> currentCultureData, IPrinter<string> printer, ILocalizationLoader datLoader, ILocalizationLoader xmlLoader, ILocalizationLoader jsonLoader, ILocalizationLoader textLoader)
        {
            Data = new Dictionary<string, IDictionary<string, string>>(1) { [Thread.CurrentThread.CurrentUICulture.Name] = currentCultureData };
            _loaders = new Dictionary<string, ILocalizationLoader>(4)
            {
                [".json"] = jsonLoader,
                [".xml"] = xmlLoader,
                [".txt"] = textLoader,
                [".dat"] = datLoader
            };
            _printer = printer;
        }
        public void AddOrSetResource(string key, string value, CultureInfo info)
        {
            if (Data[info.Name] is null && Data.Count >= 1)
            {
                _printer.Print(Data[Data.FirstOrDefault().Key]["CultureNotSupported"]);
                return;
            }
            else if (Data[info.Name] is null)
            {
                throw new ApplicationException();
            }
            else if (Data.ContainsKey(key))
            {
                Data[info.Name][key] = value;
            }
            else
            {
                Data[info.Name].Add(key, value);
            }
        }
#pragma warning disable CS1998
        public async ValueTask<bool> TryLoadData(string from)
        {
            if (!File.Exists(from))
            {
                return false;
            }
            Dictionary<string, string> dictionary = new Dictionary<string, string>(16);
            if (!_loaders[Path.GetExtension(from)].FillData(from, dictionary))
            {
                return false;
            }
            Data = new Dictionary<string, IDictionary<string, string>>(1) { [CultureInfo.CurrentUICulture.Name] = dictionary };
            return true;
        }
        public string this[string key]
        {
            get
            {
                return Data[CultureInfo.CurrentUICulture.Name][key];
            }
            set
            {
                AddOrSetResource(key, value, CultureInfo.CurrentUICulture);
            }
        }
    }

    [Serializable]
    public struct SerializableEntity
    {
        private bool _deserialized { get; set; }
        public IDictionary<string, string> Data { get; internal set; }
        public void SetData(IDictionary<string, string> data)
        {
            Data = data;
        }
        [OnDeserialized]
        internal void OnDeserialization(StreamingContext ctx)
        {
            _deserialized = true;
        }
    }
    public interface IXmlHandler
    {
        IXmlHandler SerializeToFile<T>(T serializable, string to);
        IXmlHandler DeserializeFromFile<T>(string from, out T result);
        IXmlHandler ReadXmlFileViaXElement(string from, out string xml);
        IXmlHandler ReadXmlFile(string from, out string xml);
        ValueTask<IXmlHandler> WriteXmlFile<T>(IXmlNode<T> root, string to);
        public interface IXmlNode<T>
        {
            string Name { get; }
            T Value { get; }
            bool IsParent { get; }
            bool HasAttributes { get; }
            ICollection<IXmlAttribute> Attributes { get; }
            ICollection<IXmlNode<T>> Descendants { get; }
            IXmlNode<T> Ascendant { get; }
            void AddAttribute(IXmlAttribute attribute);
            bool RemoveAttribute(IXmlAttribute attribute);
            void AddChild(IXmlNode<T> child);
            bool RemoveChild(IXmlNode<T> child);
            T this[int index] { get; }
            T this[string name] { get; }
        }
        public interface IXmlAttribute
        {
            string Name { get; }
            string Value { get; }
        }
    }
    public struct XmlHandler : IXmlHandler
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        public XmlHandler(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _localizer = localizer;
            _printer = printer;
        }
        public IXmlHandler SerializeToFile<T>(T serializable, string to)
        {
            if (File.Exists(to))
            {
                _printer.Print(_localizer["FileAlreadyExist"]);
                return default;
            }
            if (typeof(T).GetCustomAttribute<SerializableAttribute>() is null)
            {
                _printer.Print(_localizer["NotSerializableObject"]);
            }
            using (FileStream stream = new FileStream(to, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stream, serializable);
            }
            string copy = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<XmlHandler>(copy);
        }
        public IXmlHandler DeserializeFromFile<T>(string from, out T result)
        {
            if (!File.Exists(from))
            {
                _printer.Print(_localizer["FileNotFound"]);
                result = default;
                return default;
            }
            if (typeof(T).GetCustomAttribute<SerializableAttribute>() is null)
            {
                _printer.Print(_localizer["NotSerializableObject"]);
            }
            using (FileStream stream = new FileStream(from, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Write))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                result = (T)serializer.Deserialize(stream);
            }
            string copy = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<XmlHandler>(copy);
        }
        public IXmlHandler ReadXmlFileViaXElement(string from, out string xml)
        {
            XElement root = XElement.Load(from);
            xml = $"<{root.Name}";
            foreach (var attribute in root.Attributes())
            {
                xml += $" {root.Name}=\"{root.Value}\" ";
            }
            xml += ">";
            RecursiveFillXml(root, xml);
            xml += $"</{root.Name}>";
            string copy = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<XmlHandler>(copy);
        }
        private bool RecursiveFillXml(XElement current, string xml)
        {
            try
            {
                if (current.Elements() is null)
                {
                    xml += $"</{current.Name}>";
                    return true;
                }
                else
                {
                    foreach (var node in current.Elements())
                    {
                        xml += $"<{node.Name}";
                        foreach (var attribute in current.Attributes())
                        {
                            xml += $" {node.Name}=\"{node.Value}\" ";
                        }
                        xml += ">";
                        RecursiveFillXml(node, xml);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public IXmlHandler ReadXmlFile(string from, out string xml)
        {
            if (!File.Exists(from))
            {
                _printer.Print(_localizer["FileNotFound"]);
            }
            xml = "";
            using (FileStream stream = File.OpenRead(from))
            {
                using (XmlReader reader = XmlReader.Create(stream))
                {
                    reader.MoveToContent();
                    Stack<string> previousTags = new Stack<string>(16);
                    string firstTag = null;
                    firstTag = reader.Name;
                    xml += $"<{reader.Name}";
                    for (int x = 0; x < reader.AttributeCount; x++)
                    {
                        reader.MoveToAttribute(x);
                        xml += $" {reader.Name} = \"{reader.Value}\" ";
                        reader.MoveToElement();
                    }
                    xml += $">";
                    while (!reader.EOF)
                    {
                        if (!reader.Read())
                        {
                            _printer.Print(_localizer["XmlReadingError"]);
                        }
                        if (reader.NodeType == XmlNodeType.EndElement && previousTags.TryPeek(out string value) && value == reader.Name)
                        {
                            previousTags.Pop();
                            continue;
                        }
                        if (reader.NodeType == XmlNodeType.EndElement && firstTag == reader.Name)
                        {
                            previousTags.Clear();
                            xml += $"</{reader.Name}>";
                            break;
                        }
                        if (reader.NodeType == XmlNodeType.Whitespace)
                        {
                            continue;
                        }
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            xml += $" {reader.Value} ";
                        }
                        else
                        {
                            xml += $" <{reader.Name}";
                            for (int x = 0; x < reader.AttributeCount; x++)
                            {
                                reader.MoveToAttribute(x);
                                xml += $" {reader.Name} = \"{reader.Value}\" ";
                                reader.MoveToElement();
                            }
                            xml += $">";
                            previousTags.Push(reader.Name);
                        }
                    }
                    xml += $"</{reader.Name}>";
                }
            }
            string copy = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<XmlHandler>(copy);
        }
        public async ValueTask<IXmlHandler> WriteXmlFile<T>(IXmlHandler.IXmlNode<T> root, string to)
        {
            if (File.Exists(to))
            {
                _printer.Print(_localizer["FileAlreadyExist"]);
                return default;
            }
            await using (var stream = File.Create(to))
            {
                await using (var writer = XmlWriter.Create(stream, new XmlWriterSettings() { Async = true }))
                {
                    await writer.WriteStartDocumentAsync();
                    writer.WriteStartElement(root.Name);
                    if (!root.IsParent)
                    {
                        writer.WriteValue(root.Value);
                    }
                    if (root.HasAttributes)
                    {
                        for (int x = 0; x < root.Attributes.Count; x++)
                        {
                            writer.WriteAttributeString(root.Attributes.ToArray()[x].Name, root.Attributes.ToArray()[x].Value);
                        }
                    }
                    await writer.WriteEndElementAsync();
                    await writer.WriteEndDocumentAsync();
                }
            }
            string copy = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<XmlHandler>(copy);
        }

    }
    public struct XmlNode<T> : IXmlHandler.IXmlNode<T>
    {
        public string Name { get; }
        public T Value { get; }
        public bool IsParent { get; private set; }
        public bool HasAttributes { get; private set; }
        public ICollection<IXmlHandler.IXmlNode<T>> Descendants { get; }
        public ICollection<IXmlHandler.IXmlAttribute> Attributes { get; }
        public IXmlHandler.IXmlNode<T> Ascendant { get; }
        public XmlNode(string name, IXmlHandler.IXmlNode<T> ascendant = null, T value = default, ICollection<IXmlHandler.IXmlNode<T>> descendants = null, ICollection<IXmlHandler.IXmlAttribute> attributes = null)
        {
            Name = name;
            Value = value;
            Ascendant = ascendant;
            if (IsParent = (descendants is not null))
            {
                Descendants = descendants;
            }
            else
            {
                Descendants = new HashSet<IXmlHandler.IXmlNode<T>>(2, new NodeEqualityComparer());
            }
            if (HasAttributes = (attributes is not null))
            {
                Attributes = attributes;
            }
            else
            {
                Attributes = new HashSet<IXmlHandler.IXmlAttribute>(2, new AttributeEqualityComparer());
            }
        }
        public void AddChild(IXmlHandler.IXmlNode<T> child)
        {
            Descendants.Add(child);
            IsParent = true;
        }
        public bool RemoveChild(IXmlHandler.IXmlNode<T> child)
        {
            if (Descendants.Count == 1)
            {
                IsParent = false;
            }
            return Descendants.Remove(child);
        }
        public void AddAttribute(IXmlHandler.IXmlAttribute child)
        {
            Attributes.Add(child);
            HasAttributes = true;
        }
        public bool RemoveAttribute(IXmlHandler.IXmlAttribute child)
        {
            if (Attributes.Count == 1)
            {
                HasAttributes = false;
            }
            return Attributes.Remove(child);
        }
        public T this[int index]
        {
            get
            {
                int counter = 0;
                foreach (var node in Descendants)
                {
                    if (counter == index)
                    {
                        return node.Value;
                    }
                    Interlocked.Increment(ref counter);
                }
                return default;
            }
        }
        public T this[string name]
        {
            get
            {
                foreach (var node in Descendants)
                {
                    if (node.Name == name)
                    {
                        return node.Value;
                    }
                }
                return default;
            }
        }
        public struct AttributeEqualityComparer : IEqualityComparer<IXmlHandler.IXmlAttribute>
        {
            public bool Equals(IXmlHandler.IXmlAttribute arg1, IXmlHandler.IXmlAttribute arg2)
            {
                return arg1.Value == arg2.Value && arg1.Name == arg2.Name;
            }
            public int GetHashCode(IXmlHandler.IXmlAttribute xmlAttribute)
            {
                return xmlAttribute.Value.GetHashCode();
            }
        }
        public struct NodeEqualityComparer : IEqualityComparer<IXmlHandler.IXmlNode<T>>
        {
            public unsafe bool Equals(IXmlHandler.IXmlNode<T> arg1, IXmlHandler.IXmlNode<T> arg2)
            {
                if (arg1.Value.ToString().Length != arg2.Value.ToString().Length)
                {
                    return false;
                }
                bool flag = false;
                fixed (char* firstCharacter = arg1.Value.ToString())
                {
                    fixed (char* secondCharacter = arg2.Value.ToString())
                    {
                        for (int x = 0; x < arg1.Value.ToString().Length; x++)
                        {
                            flag = firstCharacter[x] == secondCharacter[x];
                        }
                    }
                }
                return flag;
            }
            public int GetHashCode(IXmlHandler.IXmlNode<T> node)
            {
                return node.Name.GetHashCode();
            }
        }
    }
}
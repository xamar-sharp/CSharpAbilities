using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using System.Text.Json;
using System.Collections;
namespace CSharpAbilities
{
#nullable disable
    public interface IAssemblyExplorer
    {
        ValueTask Explore(Assembly assembly);
    }
    public interface IAssemblyFormatter
    {
        ValueTask PrintAboutAssembly(Assembly assembly);
    }
    public interface IDomainExplorer
    {
        ValueTask Explore(AppDomain domain);
    }
    public interface IDomainFormatter
    {
        ValueTask PrintAboutDomain(AppDomain domain);
    }
    public interface IReflectionManager
    {
        ValueTask ExploreCurrentAssembly();
        ValueTask ExploreMSCoreLibrary();
        ValueTask ExploreAssemblyFrom(string physicalPathToPE32);
        ValueTask ExploreAssembliesIn(AppDomain domain);
        ValueTask FormatAssembly(Assembly assembly);
        ValueTask FormatCurrentDomain();
        ValueTask LoadAssemblyIntoDomain(string physicalPathToPE32);
        ValueTask UnloadLastAssembly();
        ValueTask ExecuteAssemblyInDomain(AppDomain domain, string physicalPathToPE32);
        ValueTask Invoke(Type instanceType, object[] ctorArgs, string methodInfoName, object[] methodArgs);
        ValueTask<object> CreateInstance(string typeName, object[] ctorArgs);
        ValueTask<bool> ImplementsInterface(string type, string interfaceType);
    }
    public readonly struct AssemblyFormatter : IAssemblyFormatter
    {
        private readonly ILocalizer<string> _localizer;
        private readonly IPrinter<string> _printer;
        private readonly StringBuilder _builder;
        private readonly static object SYNC_ROOT = new object();
        public AssemblyFormatter(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _localizer = localizer;
            _printer = printer;
            _builder = new StringBuilder(128);
        }
        public async ValueTask PrintAboutAssembly(Assembly assembly)
        {
            lock (SYNC_ROOT)
            {
                _builder.AppendLine($"{_localizer["AssemblyHashTitle"]}:{assembly.GetName().HashAlgorithm}");
                _builder.AppendLine($"{_localizer["AssemblyProcessorTitle"]}:{assembly.GetName().HashAlgorithm}");
                _builder.AppendLine($"{_localizer["AssemblyVersionTitle"]}:{assembly.GetName().Version}");
                _builder.AppendLine($"{_localizer["AssemblyLocationTitle"]}:{assembly.Location}");
            }
            await _printer.PrintAsync(_builder.ToString());
            _builder.Clear();
        }
    }
    public readonly struct ReflectionManager : IReflectionManager
    {
        private readonly ILocalizer<string> _localizer;
        private readonly IPrinter<string> _printer;
        private readonly IAssemblyExplorer _assemblyExplorer;
        private readonly IDomainExplorer _domainExplorer;
        private readonly IAssemblyFormatter _assemblyFormatter;
        private readonly IDomainFormatter _domainFormatter;
        private readonly System.Runtime.Loader.AssemblyLoadContext _ctx;
        public ReflectionManager(IPrinter<string> printer, ILocalizer<string> localizer, IAssemblyExplorer assemblyExplorer, IDomainExplorer domainExplorer, IAssemblyFormatter assemblyFormatter, IDomainFormatter domainFormatter)
        {
            _ctx = new System.Runtime.Loader.AssemblyLoadContext($"{Guid.NewGuid()}", true);
            _localizer = localizer;
            _printer = printer;
            _assemblyExplorer = assemblyExplorer;
            _domainExplorer = domainExplorer;
            _assemblyFormatter = assemblyFormatter;
            _domainFormatter = domainFormatter;
        }
        public async ValueTask<object> CreateInstance(string typeName, object[] ctorArgs)
        {
            try
            {
                return AppDomain.CurrentDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().Location, typeName, ctorArgs);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["CreateInstanceError"]);
                return null;
            }
        }
        public async ValueTask<bool> ImplementsInterface(string type, string interfaceType)
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetType(type).GetInterface(interfaceType) is not null;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ImplementsInterfaceError"]);
                return false;
            }
        }
        public async ValueTask Invoke(Type instanceType, object[] ctorArgs, string methodInfoName, object[] methodArgs)
        {
            try
            {
                object instance = Activator.CreateInstance(instanceType, ctorArgs);
                instanceType.GetMethod(methodInfoName)?.Invoke(instance, methodArgs);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["InvokeMethodError"]);
            }
        }
        public async ValueTask ExecuteAssemblyInDomain(AppDomain domain, string physicalPathToPE32)
        {
            try
            {
                domain.ExecuteAssembly(physicalPathToPE32);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ExecuteAssemblyError"]);
            }
        }
        public async ValueTask UnloadLastAssembly()
        {
            _ctx.Unload();
        }
        public async ValueTask LoadAssemblyIntoDomain(string physicalPathToPE32)
        {
            try
            {
                _ctx.LoadFromAssemblyPath(physicalPathToPE32);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["LoadAssemblyError"]);
            }
        }
        public async ValueTask ExploreCurrentAssembly()
        {
            await _assemblyExplorer.Explore(Assembly.GetExecutingAssembly());
        }
        public async ValueTask ExploreMSCoreLibrary()
        {
            await _assemblyExplorer.Explore(Assembly.GetCallingAssembly());
        }
        public async ValueTask ExploreAssemblyFrom(string physicalPathToPE32)
        {
            await _assemblyExplorer.Explore(Assembly.LoadFrom(physicalPathToPE32));
        }
        public async ValueTask ExploreAssembliesIn(AppDomain domain)
        {
            await _domainExplorer.Explore(domain);
        }
        public async ValueTask FormatAssembly(Assembly asm)
        {
            await _assemblyFormatter.PrintAboutAssembly(asm);
        }
        public async ValueTask FormatCurrentDomain()
        {
            await _domainExplorer.Explore(AppDomain.CurrentDomain);
        }
    }
    public readonly struct AppDomainExplorer : IDomainExplorer
    {
        private readonly ILocalizer<string> _localizer;
        private readonly IPrinter<string> _printer;
        private readonly StringBuilder _builder;
        private readonly static object SYNC_ROOT = new object();
        private readonly IAssemblyExplorer _explorer;
        public AppDomainExplorer(IPrinter<string> printer, ILocalizer<string> localizer, IAssemblyExplorer explorer)
        {
            _localizer = localizer;
            _printer = printer;
            _builder = new StringBuilder(128);
            _explorer = explorer;
        }
        public async ValueTask Explore(AppDomain domain)
        {
            try
            {
                foreach (var asm in domain.GetAssemblies())
                {
                    await _explorer.Explore(asm);
                }
            }
            catch
            {
                await _printer.PrintAsync(_localizer["DomainExplorerError"]);
            }
        }
    }
    public readonly struct AssemblyExplorer : IAssemblyExplorer
    {
        private readonly ILocalizer<string> _localizer;
        private readonly IPrinter<string> _printer;
        private readonly StringBuilder _builder;
        private readonly static object SYNC_ROOT = new object();
        public AssemblyExplorer(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _localizer = localizer;
            _printer = printer;
            _builder = new StringBuilder(128);
        }
        public async ValueTask Explore(Assembly assembly)
        {
            try
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    _builder.AppendLine($"{_localizer["TypeFullNameTitle"]}:{type.FullName}");
                    _builder.AppendLine($"{_localizer["TypeNameTitle"]}:{type.Name}");
                    _builder.AppendLine($"{_localizer["TypeAssemblyNameTitle"]}:{type.Assembly.Location}");
                    foreach (var property in type.GetProperties())
                    {
                        _builder.AppendLine($"{_localizer["PropertyNameTitle"]}:{property.Name}");
                        _builder.Append($"{_localizer["MethodNameTitle"]}:{property.GetSetMethod().Name}");
                        AppendParameters(_builder, property.GetSetMethod());
                        _builder.AppendLine();
                        _builder.Append($"{_localizer["MethodNameTitle"]}:{property.GetGetMethod().Name}");
                        AppendParameters(_builder, property.GetGetMethod());
                        _builder.AppendLine();
                    }
                    foreach (var @event in type.GetEvents())
                    {
                        _builder.AppendLine($"{_localizer["EventNameTitle"]}:{@event.Name}");
                        _builder.Append($"{_localizer["MethodNameTitle"]}:{@event.GetAddMethod().Name}");
                        AppendParameters(_builder, @event.GetAddMethod());
                        _builder.AppendLine();
                        _builder.Append($"{_localizer["MethodNameTitle"]}:{@event.GetRemoveMethod().Name}");
                        AppendParameters(_builder, @event.GetRemoveMethod());
                        _builder.AppendLine();
                    }
                    foreach (var field in type.GetFields())
                    {
                        _builder.AppendLine($"{_localizer["FieldNameTitle"]}:{field.Name}");
                    }
                    foreach (var method in type.GetMethods())
                    {
                        _builder.Append($"{_localizer["MethodNameTitle"]}:{method.Name}");
                        AppendParameters(_builder, method);
                        _builder.AppendLine();
                    }
                    foreach (var ctor in type.GetConstructors())
                    {
                        _builder.Append($"{_localizer["ConstructorNameTitle"]}:{ctor.Name}");
                        AppendParameters(_builder, ctor);
                        _builder.AppendLine();
                    }
                }
            }
            catch
            {
                await _printer.PrintAsync(_localizer["AssemblyExplorerError"]);
            }
            _builder.Clear();
        }
        private void AppendParameters(StringBuilder builder, MethodBase method)
        {
            foreach (var param in method.GetParameters())
            {
                _builder.Append($" {param.ParameterType} {param.Name} ");
            }
        }
    }
    public readonly struct AppDomainFormatter : IDomainFormatter
    {
        private readonly ILocalizer<string> _localizer;
        private readonly IPrinter<string> _printer;
        private readonly StringBuilder _builder;
        private readonly static object SYNC_ROOT = new object();
        public AppDomainFormatter(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _localizer = localizer;
            _printer = printer;
            _builder = new StringBuilder(128);
        }
        public async ValueTask PrintAboutDomain(AppDomain domain)
        {
            lock (SYNC_ROOT)
            {
                _builder.AppendLine($"{_localizer["DomainNameTitle"]}:{domain.FriendlyName}");
                _builder.AppendLine($"{_localizer["DomainSetupTitle"]}:{System.Text.Json.JsonSerializer.Serialize(domain.SetupInformation)}");
            }
            await _printer.PrintAsync(_builder.ToString());
            _builder.Clear();
        }
    }
    public interface IJsonHandler
    {
        ValueTask SerializeJson(object obj);
        ValueTask ReadJson(string physicalPath);
        ValueTask WriteJson(string physicalPath);
    }
    public struct JsonHandler : IJsonHandler
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        public JsonHandler(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _printer = printer;
            _localizer = localizer;
        }
        public async ValueTask SerializeJson(object obj)
        {
            try
            {
                await _printer.PrintAsync(JsonConvert.SerializeObject(obj));
            }
            catch
            {
                await _printer.PrintAsync(_localizer["JsonSerializingError"]);
            }
        }
        public async ValueTask ReadJson(string physicalPath)
        {
            try
            {
                StringBuilder builder = new(128);
                using (var stream = File.OpenText(physicalPath))
                {
                    using (JsonTextReader reader = new JsonTextReader(stream))
                    {

                        while (reader.Read())
                        {
                            switch (reader.TokenType)
                            {
                                case JsonToken.PropertyName:
                                    builder.AppendLine($"\"{reader.Value}\":");
                                    break;
                                case JsonToken.StartArray:
                                    builder.AppendLine($"[");
                                    break;
                                case JsonToken.EndArray:
                                    builder.AppendLine("]");
                                    break;
                                case JsonToken.StartObject:
                                    builder.AppendLine("{");
                                    break;
                                case JsonToken.EndObject:
                                    builder.AppendLine("}");
                                    break;
                                default:
                                    builder.Append(reader.Value);
                                    break;
                            }
                        }
                    }
                }
                await _printer.PrintAsync(builder.ToString());
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ReadingJsonError"]);
            }
        }

        public async ValueTask WriteJson(string physicalPath)
        {
            try
            {
                using (TextWriter writer = File.CreateText(physicalPath))
                {
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
                    {
                        await jsonWriter.WriteStartObjectAsync();
                        await jsonWriter.WritePropertyNameAsync("name");
                        await jsonWriter.WriteValueAsync("Net");
                        await jsonWriter.WriteCommentAsync("here json...");
                        await jsonWriter.WriteEndObjectAsync();
                    }
                }
            }
            catch
            {
                await _printer.PrintAsync(_localizer["WritingJsonError"]);
            }
        }
    }
}


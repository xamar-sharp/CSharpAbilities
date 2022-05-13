using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
#nullable disable
namespace CSharpAbilities
{
    public interface ICSharpServiceProvider
    {
        T GetService<T>(params object[] args);
        T GetRequiredService<T>(params object[] args);
    }
    public class CSharpServiceProvider : ICSharpServiceProvider
    {
        private IRepository<string> _repository;
        private IPrinter<string> _printer;
        private ILocalizer<string> _localizer;
        private IDomainExplorer _domainExplorer;
        private IDomainFormatter _domainFormatter;
        private IAssemblyFormatter _assemblyFormatter;
        private IAssemblyExplorer _assemblyExplorer;
        private IReflectionManager _reflectionManager;
        private IJsonHandler _jsonHandler;
        private IXmlHandler _xmlHandler;
        private IConnectionSetuper _connectionSetuper;
        private INetAdditional _netAdditional;
        private IUdpConnector _udpConnector;
        private ITcpConnector _tcpConnector;
        private IThreadFormatter _threadFormatter;
        private ITaskFormatter _taskFormatter;
        private IThreadManipulator _threadManipulator;
        private ITaskManipulator _taskManipulator;
        private IProcessFormatter _processFormatter;
        private IProcessManipulator _processManipulator;
        private IHttpConnector _httpConnector;
        private IHttpListener _httpListener;
        private IThreadPoolWorker _threadPoolWorker;
        private ILocalizationLoader _localizationLoader;
        public CSharpServiceProvider SetJsonHandler(IJsonHandler handler)
        {
            _jsonHandler = handler;
            return this;
        }
        public CSharpServiceProvider SetXmlHandler(IXmlHandler handler)
        {
            _xmlHandler = handler;
            return this;
        }
        public CSharpServiceProvider SetReflectionManager(IReflectionManager manager)
        {
            _reflectionManager = manager;
            return this;
        }
        public CSharpServiceProvider SetAssemblyExplorer(IDomainExplorer explorer)
        {
            _domainExplorer = explorer;
            return this;
        }
        public CSharpServiceProvider SetAssemblyFormatter(IDomainFormatter formatter)
        {
            _domainFormatter = formatter;
            return this;
        }
        public CSharpServiceProvider SetThreadFormatter(IThreadFormatter formatter)
        {
            _threadFormatter = formatter;
            return this;
        }
        public CSharpServiceProvider SetTaskFormatter(ITaskFormatter formatter)
        {
            _taskFormatter = formatter;
            return this;
        }
        public CSharpServiceProvider SetAssemblyExplorer(IAssemblyExplorer explorer)
        {
            _assemblyExplorer = explorer;
            return this;
        }
        public CSharpServiceProvider SetAssemblyFormatter(IAssemblyFormatter formatter)
        {
            _assemblyFormatter = formatter;
            return this;
        }
        public CSharpServiceProvider SetLocalizationLoader(ILocalizationLoader loader)
        {
            _localizationLoader = loader;
            return this;
        }
        public CSharpServiceProvider SetThreadPoolWorker(IThreadPoolWorker worker)
        {
            _threadPoolWorker = worker;
            return this;
        }
        public CSharpServiceProvider SetProcessManipulator(IProcessManipulator manipulator)
        {
            _processManipulator = manipulator;
            return this;
        }
        public CSharpServiceProvider SetThreadManipulator(IThreadManipulator manipulator)
        {
            _threadManipulator = manipulator;
            return this;
        }
        public CSharpServiceProvider SetNetAdditional(INetAdditional additional)
        {
            _netAdditional = additional;
            return this;
        }
        public CSharpServiceProvider SetTaskManipulator(ITaskManipulator manipulator)
        {
            _taskManipulator = manipulator;
            return this;
        }
        public CSharpServiceProvider SetProcessFormatter(IProcessFormatter formatter)
        {
            _processFormatter = formatter;
            return this;
        }
        public CSharpServiceProvider SetTcpConnector(ITcpConnector connector)
        {
            _tcpConnector = connector;
            return this;
        }
        public CSharpServiceProvider SetPrinter(IPrinter<string> printer)
        {
            _printer = printer;
            return this;
        }
        public CSharpServiceProvider SetLocalizer(ILocalizer<string> localizer)
        {
            _localizer = localizer;
            return this;
        }

        public CSharpServiceProvider SetHttpConnector(IHttpConnector connector)
        {
            _httpConnector = connector;
            return this;
        }
        public CSharpServiceProvider SetUdpConnector(IUdpConnector connector)
        {
            _udpConnector = connector;
            return this;
        }

        public T GetService<T>(params object[] args)
        {
            return (T)(GetType().GetField(typeof(T).FullName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this));
        }
        public T GetRequiredService<T>(params object[] args)
        {
            return (T)(GetType().GetField(typeof(T).FullName!, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this))!;
        }
    }
}

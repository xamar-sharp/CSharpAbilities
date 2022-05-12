using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
#nullable disable
namespace CSharpAbilities
{
    public interface IEnvironmentBrowser
    {
        ValueTask Browse(TimeSpan dueTime);
        DirectoryInfo GetFolder(Environment.SpecialFolder targetFolder);
        void Exit(int exitCode);
    }

    public struct EnvironmentBrowser : IEnvironmentBrowser
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        public EnvironmentBrowser(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _printer = printer;
            _localizer = localizer;
        }
        public void Exit(int exitCode)
        {
            Environment.Exit(exitCode);
        }
        public DirectoryInfo GetFolder(Environment.SpecialFolder targetFolder)
        {
            return new DirectoryInfo(Environment.GetFolderPath(targetFolder));
        }
        public async ValueTask Browse(TimeSpan dueTime)
        {
            await Task.Delay(dueTime);
            StringBuilder builder = new StringBuilder(128);
            builder.AppendLine($"{_localizer["EnvironmentProcessorCountTitle"]}:{Environment.ProcessorCount}");
            builder.AppendLine($"{_localizer["EnvironmentCurrentDirectoryTitle"]}:{Environment.CurrentDirectory}");
            builder.AppendLine($"{_localizer["EnvironmentProcessIdTitle"]}:{Environment.ProcessId}");
            builder.AppendLine($"{_localizer["EnvironmentTickCountTitle"]}:{Environment.TickCount64}");
            builder.AppendLine($"{_localizer["EnvironmentOperatingVersionTitle"]}:{Environment.OSVersion.VersionString}");
            builder.AppendLine($"{_localizer["EnvironmentOperatingIs64Title"]}:{Environment.Is64BitOperatingSystem}");
            builder.AppendLine($"{_localizer["EnvironmentProcessIs64Title"]}:{Environment.Is64BitProcess}");
            builder.AppendLine($"{_localizer["EnvironmentUserNameTitle"]}:{Environment.UserName}");
            builder.AppendLine($"{_localizer["EnvironmentMachineNameTitle"]}:{Environment.MachineName}");
            builder.AppendLine($"{_localizer["EnvironmentNewLineTitle"]}:{Environment.NewLine}");
            builder.AppendLine($"{_localizer["EnvironmentCommandLineTitle"]}:{JsonSerializer.Serialize(Environment.GetCommandLineArgs())}");
            builder.AppendLine($"{_localizer["EnvironmentLogicalDrivesTitle"]}:{JsonSerializer.Serialize(Environment.GetLogicalDrives())}");
            await _printer.PrintAsync(builder.ToString());
        }
    }
    public delegate void GCCallback(int generation = -1, GCCollectionMode mode = GCCollectionMode.Default, object getGeneration = null, int memoryToNoGCRegion = -1, object toKeepAlive = null, object forFinalize = null);
    public interface IGCWorker
    {
        event GCCallback WorkerRequested;
        IGCWorker SetCollect();
        IGCWorker SetKeepAlive();
        IGCWorker SetGetGeneration();
        IGCWorker SetStartNoGCRegion();
        IGCWorker SetEndNoGCRegion();
        IGCWorker SetFullFinalize();
        IGCWorker SetGetTotalMemory();
        IGCWorker SetSuppressFinalize();
        IGCWorker SetReRegisterForFinalize();
        IGCWorker ExecuteWorker(int generation = -1, bool isForced = false, object getGeneration = null, int memoryToNoGCRegion = -1, object toKeepAlive = null, object forFinalize = null);
    }
    public struct GCWorker : IGCWorker
    {
        public event GCCallback WorkerRequested;
        private readonly IPrinter<string> _printer;
        public GCWorker(IPrinter<string> printer)
        {
            _printer = printer;
            WorkerRequested = (gen, forced, getGen, memory, toKeep, forFin) =>
             {

             };
        }
        public IGCWorker ExecuteWorker(int generation = -1, bool isForced = false, object getGeneration = null, int memoryToNoGCRegion = -1, object toKeepAlive = null, object forFinalize = null)
        {
            WorkerRequested?.Invoke(generation, isForced ? GCCollectionMode.Forced : GCCollectionMode.Optimized, getGeneration, memoryToNoGCRegion, toKeepAlive, forFinalize);
            return this;
        }
        public IGCWorker SetSuppressFinalize()
        {
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                GC.SuppressFinalize(forFin);
            };
            return this;
        }
        public IGCWorker SetReRegisterForFinalize()
        {
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                GC.ReRegisterForFinalize(forFin);
            };
            return this;
        }
        public IGCWorker SetGetTotalMemory()
        {
            IPrinter<string> copy = _printer;
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                copy.Print(GC.GetTotalMemory(false).ToString());
            };
            return this;
        }
        public IGCWorker SetCollect()
        {
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                GC.Collect(gen, forced);
            };
            return this;
        }
        public IGCWorker SetKeepAlive()
        {
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                GC.KeepAlive(toKeep);
            };
            return this;
        }
        public IGCWorker SetGetGeneration()
        {
            IPrinter<string> copy = _printer;
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                copy.Print(GC.GetGeneration(getGen).ToString());
            };
            return this;
        }
        public IGCWorker SetStartNoGCRegion()
        {
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                GC.TryStartNoGCRegion(memory);
            };
            return this;
        }
        public IGCWorker SetEndNoGCRegion()
        {
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                GC.EndNoGCRegion();
            };
            return this;
        }
        public IGCWorker SetFullFinalize()
        {
            WorkerRequested += (gen, forced, getGen, memory, toKeep, forFin) =>
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
            };
            return this;
        }
    }
    public interface IThreadManipulator:IDisposable
    {
        void KillThread();
        void KillThread(Thread thread);
        Thread BuildThread(Action<object> action,string threadName,int stackSize, ThreadPriority priority, CultureInfo culture, CultureInfo uiCulture, bool isBackground);
        void StartNewThread(Action<object> action,object obj,string threadName, int stackSize,ThreadPriority priority, CultureInfo culture, CultureInfo uiCulture, bool isBackground, ApartmentState state = ApartmentState.MTA);
        void StopThread(int milliseconds);
        void StopThread(int milliseconds, Thread thread);
        void SleepThread(int milliseconds);
        void SkipIterations(int count);
        void EnableMemoryBarrier();
        void EnableMemoryBarrierProcessWide();
        ValueTask AboutThread(Thread thread);
        ValueTask AboutThread();
        void ModifyCurrentThread(string threadName, ThreadPriority priority, CultureInfo culture, CultureInfo uiCulture, bool isBackground, ApartmentState state = ApartmentState.MTA);
        void ExecuteCallbackWithThreadCulture(Thread thread, Action<object> callback, object param);
        void ExecuteOnSynchronizationContext(SynchronizationContext ctx, Action<object> obj, object param);
        Action<object> CreateThreadSafeCallback(Action<object> callback, ThreadSafeMethod method);
        ValueTask StartTimer(int milliseconds,Predicate<object> condition, Action<object> callback, object param);
        int GetCurrentProcessorId();
    }
    public enum ThreadSafeMethod
    {
        Mutex,Monitor,AutoResetEvent,SemaphoreSlim
    }
    public interface IThreadFormatter
    {
        ValueTask FormatThread(Thread thread);
    }
    public readonly struct ThreadFormatter : IThreadFormatter
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        public ThreadFormatter(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _printer = printer;
            _localizer = localizer;
        }
        public async ValueTask FormatThread(Thread thread)
        {
            StringBuilder builder = new StringBuilder(128);
            builder.AppendLine($"{_localizer["ThreadNameTitle"]}:{thread.Name}");
            builder.AppendLine($"{_localizer["ThreadIsBackgroundTitle"]}:{thread.IsBackground}");
            builder.AppendLine($"{_localizer["ThreadStateTitle"]}:{thread.ThreadState}");
            builder.AppendLine($"{_localizer["ThreadPriorityTitle"]}:{thread.Priority}");
            builder.AppendLine($"{_localizer["ThreadCultureTitle"]}:{thread.CurrentCulture.EnglishName}");
            builder.AppendLine($"{_localizer["ThreadUICultureTitle"]}:{thread.CurrentUICulture.EnglishName}");
            builder.AppendLine($"{_localizer["ThreadIdTitle"]}:{thread.ManagedThreadId}");
            await _printer.PrintAsync(builder.ToString());
        }
    }
    public readonly struct ThreadManipulator:IThreadManipulator
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        private readonly IThreadFormatter _formatter;
        private readonly Mutex _mutex;
        private readonly AutoResetEvent _event;
        private readonly SemaphoreSlim _semaphore;
        public static readonly object SYNC_ROOT = new object();
        public ThreadManipulator(IPrinter<string> printer,ILocalizer<string> localizer,IThreadFormatter formatter)
        {
            _formatter = formatter;
            _printer = printer;
            _localizer = localizer;
            _mutex = new Mutex();
            _event = new AutoResetEvent(true);
            _semaphore = new SemaphoreSlim(0, 1);
        }
        public ThreadManipulator(IPrinter<string> printer, ILocalizer<string> localizer, IThreadFormatter formatter,string mutexName)
        {
            _printer = printer;
            _localizer = localizer;
            _formatter = formatter;
            _mutex = Mutex.OpenExisting(mutexName);
            _event = new AutoResetEvent(true);
            _semaphore = new SemaphoreSlim(0, 1);
        }
        public void ModifyCurrentThread(string threadName, ThreadPriority priority, CultureInfo culture, CultureInfo uiCulture, bool isBackground, ApartmentState state = ApartmentState.MTA)
        {
            Thread thread = Thread.CurrentThread;
            thread.Name = threadName;
            thread.Priority = priority;
            thread.CurrentCulture = culture;
            thread.CurrentUICulture = uiCulture;
            thread.IsBackground = isBackground;
            thread.TrySetApartmentState(state);
        }
        public void StartNewThread(Action<object> action, object obj, string threadName, int stackSize, ThreadPriority priority, CultureInfo culture, CultureInfo uiCulture, bool isBackground, ApartmentState state = ApartmentState.MTA)
        {
            Thread thread=BuildThread(action, threadName, stackSize, priority, culture, uiCulture, isBackground);
            thread.TrySetApartmentState(state);
            thread.Start(obj);
        }
        public async ValueTask StartTimer(int milliseconds,Predicate<object> condition, Action<object> callback, object param)
        {
            await using Timer timer = new Timer((obj) => callback?.Invoke(obj), param, 0, milliseconds);
            while (condition.Invoke(param))
            {
                await Task.Delay(milliseconds);
            }
        }
        public void StopThread(int milliseconds, Thread thread)
        {
            thread.Join(milliseconds);
        }
        public void StopThread(int milliseconds)
        {
            Thread.CurrentThread.Join(milliseconds);
        }
        public void KillThread(Thread thread)
        {
            thread.Join(10);
            thread.Interrupt();
        }
        public void KillThread()
        {
            Thread thread = Thread.CurrentThread;
            thread.Join(10);
            thread.Interrupt();
        }
        public Thread BuildThread(Action<object> action, string threadName, int stackSize, ThreadPriority priority, CultureInfo culture, CultureInfo uiCulture, bool isBackground)
        {
            var thread = new Thread((obj) => action?.Invoke(obj), stackSize) { IsBackground = isBackground, Priority = priority, CurrentCulture = culture, CurrentUICulture = uiCulture };
            return thread;
        }
        public void SleepThread(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
        public int GetCurrentProcessorId()
        {
            return Thread.GetCurrentProcessorId();
        }
        public void Dispose()
        {
            _mutex.Dispose();
            _event.Dispose();
            _semaphore.Dispose();
            new GCWorker().SetCollect().ExecuteWorker(GC.MaxGeneration, true, null, -1, null, null);
        }
        public void SkipIterations(int count)
        {
            Thread.SpinWait(count);
        }
        public Action<object> CreateThreadSafeCallback(Action<object> callback, ThreadSafeMethod method)
        {
            Action<object> result;
            switch (method)
            {
                case ThreadSafeMethod.Monitor:
                    result = (obj) =>
                    {
                        try
                        {
                            Monitor.Enter(SYNC_ROOT);
                            callback?.Invoke(obj);
                        }
                        finally
                        {
                            Monitor.Exit(SYNC_ROOT);
                        }
                    };
                    break;
                case ThreadSafeMethod.AutoResetEvent:
                    AutoResetEvent @event = _event;
                    result = (obj) =>
                    {
                        try
                        {
                            @event.WaitOne();
                            callback?.Invoke(obj);
                        }
                        finally
                        {
                            @event.Set();
                        }
                    };
                    break;
                case ThreadSafeMethod.Mutex:
                    Mutex mutex = _mutex;
                    result = (obj) =>
                    {
                        try
                        {
                            mutex.WaitOne();
                            callback?.Invoke(obj);
                        }
                        finally
                        {
                            mutex.ReleaseMutex();
                        }
                    };
                    break;
                default:
                    SemaphoreSlim slim = _semaphore;
                    result = (obj) =>
                    {
                        try
                        {
                            slim.Wait();
                            callback?.Invoke(obj);
                        }
                        finally
                        {
                            slim.Release();
                        }
                    };
                    break;
            }
            return result;
        }
        public void ExecuteCallbackWithThreadCulture(Thread thread, Action<object> callback, object param)
        {
            ExecutionContext.Run(thread.ExecutionContext, (obj)=>callback?.Invoke(obj), param);
        }
        public void ExecuteOnSynchronizationContext(SynchronizationContext ctx, Action<object> callback, object param)
        {
            ctx.Post((parameter) => callback?.Invoke(parameter), param);
        }
        public void EnableMemoryBarrier()
        {
            Thread.MemoryBarrier();
        }
        public void EnableMemoryBarrierProcessWide()
        {
            Interlocked.MemoryBarrierProcessWide();
        }
        public async ValueTask AboutThread()
        {
            await _formatter.FormatThread(Thread.CurrentThread);
        }
        public async ValueTask AboutThread(Thread thread)
        {
            await _formatter.FormatThread(thread);
        }
    }
}

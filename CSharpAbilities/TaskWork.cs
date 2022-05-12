using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable
namespace CSharpAbilities
{
    public interface ITaskManipulator
    {
        void ExecuteTaskViaThreadPool(Action<object> action, object obj);
        ValueTask CreateTaskViaTaskFactory(Action<object> action, object obj, CancellationToken token, TaskCreationOptions options, TaskScheduler scheduler);
        ValueTask CreateTaskFast(Action action, CancellationToken token);
        ValueTask CreateTaskStandard(Action<object> action, object obj, CancellationToken token, TaskCreationOptions options);
        ValueTask CreateCompletedTask<T>(TaskConfiguration config, T result, CancellationToken token = default, Exception ex = null);
        ValueTask PrintTasksState();
        ValueTask StartTask(Task task, TaskScheduler scheduler, TaskStartSetting setting);
        ValueTask<T> StartTask<T>(Task<T> task, TaskScheduler scheduler, TaskStartSetting setting);
        ValueTask AboutTask(Task task);
        ValueTask PrintThreadPoolState();
        void ParallelInvoke(CancellationToken token = default, TaskScheduler scheduler = null, int maxDegreeOfParallelism = 2, params Action[] actions);
        bool ParallelForEach<T>(IEnumerable<T> set, Action<T> handle, CancellationToken token = default, TaskScheduler scheduler = null, int maxDegreeOfParallelism = 2, bool peekLoopState = false);
        bool ParallelFor(int start, int end, Action<int> handle, CancellationToken token = default, TaskScheduler scheduler = null, int maxDegreeOfParallelism = 2, bool peekLoopState = false);
        ValueTask AsyncSleep(int milliseconds);
        ValueTask GetAwaitableObject();
        ValueTask ContinueWith<T>(Task<T> task, Action<Task<T>, object> action, object param, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler);
        ValueTask ContinueWith(Task[] tasks, Action<Task[]> action, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler);
        ValueTask ContinueWith(Task[] tasks, Action<Task> action, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler);
        ValueTask Wait(Task[] tasks, TaskWaitOption config);
        ValueTask ChangeThreadPoolState(int minThreads, int maxThreads, int minThreadsIO, int maxThreadsIO);
        ParallelQuery<T> BeginParallelQuery<T>(IEnumerable<T> source, CancellationToken token, bool isOrdered);
    }
    public interface ITaskFormatter
    {
        ValueTask PrintAboutTask(Task task);
        ValueTask PrintTasksState();
    }
    public readonly struct TaskFormatter : ITaskFormatter
    {
        private readonly ILocalizer<string> _localizer;
        private readonly IPrinter<string> _printer;
        public TaskFormatter(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _localizer = localizer;
            _printer = printer;
        }
        public async ValueTask PrintAboutTask(Task task)
        {
            StringBuilder builder = new StringBuilder(64);
            builder.AppendLine($"{_localizer["TaskIdTitle"]}:{task.Id}");
            builder.AppendLine($"{_localizer["TaskAsyncStateTitle"]}:{task.AsyncState}");
            builder.AppendLine($"{_localizer["TaskExceptionTitle"]}:{task.Exception}");
            builder.AppendLine($"{_localizer["TaskStateTitle"]}:{task.Status}");
            builder.AppendLine($"{_localizer["TaskIsFaultedTitle"]}:{task.IsFaulted}");
            builder.AppendLine($"{_localizer["TaskIsCompletedTitle"]}:{task.IsCompleted}");
            builder.AppendLine($"{_localizer["TaskIsCanceledTitle"]}:{task.IsCanceled}");
            builder.AppendLine($"{_localizer["TaskIsCompletedSuccesfullyTitle"]}:{task.IsCompletedSuccessfully}");
            await _printer.PrintAsync(builder.ToString());
        }
        public async ValueTask PrintTasksState()
        {
            StringBuilder builder = new StringBuilder(64);
            builder.AppendLine($"{_localizer["TaskCurrentIdTitle"]}:{Task.CurrentId}");
            builder.AppendLine($"{_localizer["TaskCompletedTaskIdTitle"]}:{Task.CompletedTask?.Id}");
            await _printer.PrintAsync(builder.ToString());
        }
    }
    public readonly struct TaskManipulator : ITaskManipulator
    {
        private readonly IThreadPoolWorker _worker;
        private readonly ITaskFormatter _formatter;
        public TaskManipulator(IThreadPoolWorker worker, ITaskFormatter formatter)
        {
            _worker = worker;
            _formatter = formatter;
        }
        public async ValueTask AboutTask(Task task)
        {
            await _formatter.PrintAboutTask(task);
        }
        public async ValueTask PrintTasksState()
        {
            await _formatter.PrintTasksState();
        }
        public ValueTask CreateTaskStandard(Action<object> action, object obj, CancellationToken token, TaskCreationOptions options)
        {
            return new ValueTask(new Task(action, obj, token, options));
        }
        public ValueTask CreateTaskFast(Action action, CancellationToken token)
        {
            return new ValueTask(Task.Run(action, token));
        }
        public async ValueTask StartTask(Task task, TaskScheduler scheduler, TaskStartSetting setting)
        {
            switch (setting)
            {
                case TaskStartSetting.Synchronously:
                    task.RunSynchronously(scheduler);
                    break;
                case TaskStartSetting.Asynchronously:
                    task.Start(scheduler);
                    await task;
                    break;
                default:
                    task.Start(scheduler);
                    break;
            }
        }
        public async ValueTask<T> StartTask<T>(Task<T> task, TaskScheduler scheduler, TaskStartSetting setting)
        {
            switch (setting)
            {
                case TaskStartSetting.Synchronously:
                    task.RunSynchronously(scheduler);
                    return task.GetAwaiter().GetResult();
                case TaskStartSetting.Asynchronously:
                    task.Start(scheduler);
                    return await task;
                default:
                    task.Start(scheduler);
                    return task.GetAwaiter().GetResult();
            }
        }
        public ValueTask CreateTaskViaTaskFactory(Action<object> action, object obj, CancellationToken token, TaskCreationOptions options, TaskScheduler scheduler)
        {
            return new ValueTask(Task.Factory.StartNew(action, obj, token, options, scheduler));
        }
        public ValueTask CreateCompletedTask<T>(TaskConfiguration config, T result, CancellationToken token = default, Exception ex = null)
        {
            switch (config)
            {
                case TaskConfiguration.Faulted:
                    return new ValueTask(Task.FromException(ex));
                case TaskConfiguration.Canceled:
                    return new ValueTask(Task.FromCanceled(token));
                default:
                    return new ValueTask(Task.FromResult(result));
            }
        }
        public async ValueTask PrintThreadPoolState()
        {
            await _worker.PrintThreadPoolState();
        }
        public void ParallelInvoke(CancellationToken token = default, TaskScheduler scheduler = null, int maxDegreeOfParallelism = 2, params Action[] actions)
        {
            Parallel.Invoke(new ParallelOptions() { TaskScheduler = scheduler, CancellationToken = token, MaxDegreeOfParallelism = maxDegreeOfParallelism }, actions);
        }
        public bool ParallelForEach<T>(IEnumerable<T> set, Action<T> handle, CancellationToken token = default, TaskScheduler scheduler = null, int maxDegreeOfParallelism = 2, bool peekLoopState = false)
        {
            if (peekLoopState)
            {
                return Parallel.ForEach(set, new ParallelOptions() { TaskScheduler = scheduler, CancellationToken = token, MaxDegreeOfParallelism = maxDegreeOfParallelism }, (value, state) =>
                {
                    try
                    {
                        handle.Invoke(value);
                    }
                    catch
                    {
                        state.Break();
                    }
                }).IsCompleted;
            }
            else
            {
                return Parallel.ForEach(set, new ParallelOptions() { TaskScheduler = scheduler, CancellationToken = token, MaxDegreeOfParallelism = maxDegreeOfParallelism }, (value) =>
                {
                    handle.Invoke(value);
                }).IsCompleted;
            }
        }
        public async ValueTask Wait(Task[] tasks, TaskWaitOption option)
        {
            switch (option)
            {
                case TaskWaitOption.TAP:
                    await Task.WhenAll(tasks);
                    break;
                default:
                    Task.WaitAll(tasks);
                    break;
            }
        }
        public bool ParallelFor(int start, int end, Action<int> handle, CancellationToken token = default, TaskScheduler scheduler = null, int maxDegreeOfParallelism = 2, bool peekLoopState = false)
        {
            if (peekLoopState)
            {
                return Parallel.For(start, end, new ParallelOptions() { TaskScheduler = scheduler, CancellationToken = token, MaxDegreeOfParallelism = maxDegreeOfParallelism }, (value, state) =>
                  {
                      try
                      {
                          handle.Invoke(value);
                      }
                      catch
                      {
                          state.Break();
                      }
                  }).IsCompleted;
            }
            else
            {
                return Parallel.For(start, end, new ParallelOptions() { TaskScheduler = scheduler, CancellationToken = token, MaxDegreeOfParallelism = maxDegreeOfParallelism }, (value) =>
                {
                    handle.Invoke(value);
                }).IsCompleted;
            }
        }
        public async ValueTask AsyncSleep(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
        public async ValueTask ContinueWith<T>(Task<T> task, Action<Task<T>, object> action, object param, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler)
        {
            await task.ContinueWith(action, param, token, options, scheduler);
        }
        public async ValueTask ContinueWith(Task[] tasks, Action<Task> action, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler)
        {
            await new TaskFactory(token, TaskCreationOptions.None, options, scheduler).ContinueWhenAny(tasks, action);
        }
        public async ValueTask ContinueWith(Task[] tasks, Action<Task[]> action, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler)
        {
            await new TaskFactory(token, TaskCreationOptions.None, options, scheduler).ContinueWhenAll(tasks, action);
        }
        public async ValueTask ChangeThreadPoolState(int minThreads, int maxThreads, int minThreadsIO, int maxThreadsIO)
        {
            await _worker.ChangeThreadPoolState(minThreads, maxThreads, minThreadsIO, maxThreadsIO);
        }
        public void ExecuteTaskViaThreadPool(Action<object> callback, object param)
        {
            ThreadPool.QueueUserWorkItem((obj) => callback?.Invoke(obj), param);
        }
        public async ValueTask GetAwaitableObject()
        {
            await Task.Yield();
        }
        public ParallelQuery<T> BeginParallelQuery<T>(IEnumerable<T> source, CancellationToken token, bool isOrdered)
        {
            if (isOrdered)
            {
                return source.AsParallel().AsOrdered().WithCancellation(token);
            }
            return source.AsParallel().WithCancellation(token);
        }
    }
    public interface IThreadPoolWorker
    {
        ValueTask PrintThreadPoolState();
        ValueTask ChangeThreadPoolState(int minThreads, int maxThreads, int minThreadsIO, int maxThreadsIO);
    }
    public readonly struct ThreadPoolWorker
    {
        private readonly ILocalizer<string> _localizer;
        private readonly IPrinter<string> _printer;
        public ThreadPoolWorker(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _localizer = localizer;
            _printer = printer;
        }
        public async ValueTask PrintThreadPoolState()
        {
            ThreadPool.GetMinThreads(out int workers, out int io);
            ThreadPool.GetAvailableThreads(out int availableWorkers, out int availableIO);
            ThreadPool.GetMaxThreads(out int maxWorkers, out int maxIO);
            await _printer.PrintAsync($"{_localizer["ThreadPoolMin"]}:{workers} - {io}\n{_localizer["ThreadPoolAvailable"]}:{availableWorkers} - {availableIO}" + Environment.NewLine +
                $"{_localizer["ThreadPoolMax"]}:{maxWorkers} - {maxIO}\n{_localizer["ThreadPoolCompleted"]}:{ThreadPool.CompletedWorkItemCount}\n{_localizer["ThreadPoolPending"]}:{ThreadPool.PendingWorkItemCount}" + Environment.NewLine +
                $"{_localizer["ThreadPoolThreadCount"]}:{ThreadPool.ThreadCount}");
        }
        public async ValueTask ChangeThreadPoolState(int minThreads, int maxThreads, int minThreadsIO, int maxThreadsIO)
        {
            try
            {
                ThreadPool.SetMinThreads(minThreads, minThreadsIO);
                ThreadPool.SetMaxThreads(maxThreads, maxThreadsIO);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ThreadPoolError"]);
            }
        }
    }
    public enum TaskWaitOption
    {
        TAP,
        TPL
    }
    public enum TaskStartSetting
    {
        Synchronously, Asynchronously, Parallel
    }
    public enum TaskConfiguration
    {
        Faulted, Canceled, Result
    }
}

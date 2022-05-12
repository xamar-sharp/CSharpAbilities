using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Security;
using System.Text;
using System.Linq;
using System.IO;
namespace CSharpAbilities
{
#nullable disable
    public interface IProcessManipulator
    {
        ValueTask AboutProcess();
        ValueTask AboutProcess(int id);
        ValueTask<string> ReadErrorMessage(int id);
        ValueTask<string> ReadOutputMessage(int id);
        ValueTask WriteInputMessage(int id, string message);
        ValueTask AboutProcess(string name);
        ValueTask WatchProcesses();
        ValueTask KillProcess(int id, bool includeProcessTree = false);
        ValueTask KillProcess(string name, bool includeProcessTree = false);
        ValueTask CloseProcessMainWindow(int id);
        ValueTask CloseProcessMainWindow(string name);
        void StartProcess(string processExeFile, ProcessWindowStyle style, IEnumerable<string> arguments, KeyValuePair<string, string> credentials=default);
        ValueTask ExecuteInDebugMode<T>(Action<T> callback, T parameter);
        ValueTask BoostProcessPriority(int id);
        ValueTask BoostProcessPriority(string name);
        ValueTask SetIdealProcessorForThread(int threadId, int processor);
        ValueTask SetIdealProcessorForThread(int processId,int threadId, int processor);
    }
    public interface IProcessFormatter
    {
        ValueTask PrintProcessInfo(Process process);
        void SetIdealProcessor(ProcessThread thread, int validProcessor);
    }
    public struct ProcessFormatter : IProcessFormatter
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        public ProcessFormatter(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _printer = printer;
            _localizer = localizer;
        }
        public void SetIdealProcessor(ProcessThread thread, int validProcessor)
        {
            thread.IdealProcessor = validProcessor;
        }
        public async ValueTask PrintProcessInfo(Process process)
        {
            try
            {
                StringBuilder builder = new StringBuilder(128);
                builder.AppendLine($"{_localizer["ProcessIdTitle"]}:{process.Id}");
                builder.AppendLine($"{_localizer["ProcessNameTitle"]}:{process.ProcessName}");
                builder.AppendLine($"{_localizer["ProcessPriorityTitle"]}:{process.BasePriority}");
                builder.AppendLine($"{_localizer["ProcessPhysicalMemoryTitle"]}:{process.WorkingSet64}");
                builder.AppendLine($"{_localizer["ProcessVirtualMemoryTitle"]}:{process.VirtualMemorySize64}");
                builder.AppendLine($"{_localizer["ProcessRespondingTitle"]}:{process.Responding}");
                builder.AppendLine($"{_localizer["ProcessCodeExecutingTitle"]}:{process.UserProcessorTime}");
                builder.AppendLine($"{_localizer["ProcessWaitTitle"]}:{process.TotalProcessorTime}");
                builder.AppendLine($"{_localizer["ProcessHandleTitle"]}:{process.Handle}");
                builder.AppendLine($"{_localizer["ProcessMainWindowTitle"]}:{process.MainWindowTitle}");
                builder.AppendLine($"{_localizer["ProcessStartTimeTitle"]}:{process.StartTime}");
                builder.AppendLine($"{_localizer["ProcessExitTimeTitle"]}:{process.ExitTime}");
                builder.AppendLine($"{_localizer["ProcessExitCodeTitle"]}:{process.ExitCode}");
                builder.AppendLine($"{_localizer["ProcessMachineNameTitle"]}:{process.MachineName}");
                foreach (var thread in process.Threads)
                {
                    builder.AppendLine($"{_localizer["ProcessThreadIdTitle"]}:{(thread as ProcessThread)!.Id}");
                    builder.AppendLine($"{_localizer["ProcessThreadStateTitle"]}:{(thread as ProcessThread)!.ThreadState}");
                    builder.AppendLine($"{_localizer["ProcessThreadPriorityTitle"]}:{(thread as ProcessThread)!.BasePriority}");
                    builder.AppendLine($"{_localizer["ProcessThreadWaitReasonTitle"]}:{((thread as ProcessThread)!.ThreadState == System.Diagnostics.ThreadState.Wait?(thread as ProcessThread)!.WaitReason:_localizer["ProcessThreadRunning"])}");
                    builder.AppendLine($"{_localizer["ProcessThreadUserProcessorTitle"]}:{((thread as ProcessThread)!.ThreadState != System.Diagnostics.ThreadState.Running? (thread as ProcessThread)!.UserProcessorTime : _localizer["ProcessThreadRunning"])}");
                    builder.AppendLine($"{_localizer["ProcessThreadTotalProcessorTitle"]}:{((thread as ProcessThread)!.ThreadState != System.Diagnostics.ThreadState.Running ? (thread as ProcessThread)!.TotalProcessorTime : _localizer["ProcessThreadRunning"])}");
                }
                await _printer.PrintAsync(builder.ToString());
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessAccessDeniedError"]);
            }
            finally
            {
                process.Dispose();
            }
        }
    }
    public readonly struct ProcessManipulator : IProcessManipulator
    {
        private readonly ILocalizer<string> _localizer;
        private readonly IPrinter<string> _printer;
        private readonly IProcessFormatter _formatter;
        public ProcessManipulator(IPrinter<string> printer, ILocalizer<string> localizer, IProcessFormatter formatter)
        {
            _printer = printer;
            _localizer = localizer;
            _formatter = formatter;
        }
        public async ValueTask WriteInputMessage(int id, string message)
        {
            try
            {
                await Process.GetProcessById(id).StandardInput.WriteLineAsync(message);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessWriteError"]);
            }
        }
        public async ValueTask SetIdealProcessorForThread(int processId,int threadId, int processor)
        {
            try
            {
                _formatter.SetIdealProcessor(Process.GetProcessById(processId).Threads[threadId - 1], processor <= Environment.ProcessorCount ? processor : 1);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessIdealProcessorError"]);
            }
        }
        public async ValueTask SetIdealProcessorForThread(int threadId, int processor)
        {
            try
            {
                _formatter.SetIdealProcessor(Process.GetCurrentProcess().Threads[threadId - 1],processor<=Environment.ProcessorCount?processor:1);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessIdealProcessorError"]);
            }
        }
        public async ValueTask BoostProcessPriority(int id)
        {
            try
            {
                Process.GetProcessById(id).PriorityBoostEnabled = true;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessBoostError"]);
            }
        }
        public async ValueTask BoostProcessPriority(string name)
        {
            try
            {
                Process.GetProcessesByName(name)[0].PriorityBoostEnabled = true;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessBoostError"]);
            }
        }
        public async ValueTask<string> ReadOutputMessage(int id)
        {
            try
            {
                return await Process.GetProcessById(id).StandardOutput.ReadLineAsync();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessReadError"]);
                return _localizer["ProcessReadError"];
            }
        }
        public async ValueTask<string> ReadErrorMessage(int id)
        {
            try
            {
                return await Process.GetProcessById(id).StandardError.ReadLineAsync();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessReadError"]);
                return _localizer["ProcessReadError"];
            }
        }
        public async ValueTask KillProcess(int id,bool includeProcessTree)
        {
            try
            {
                Process.GetProcessById(id)?.Kill(includeProcessTree);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessKillError"]);
            }
        }
        public unsafe void StartProcess(string processExeFile, ProcessWindowStyle style, IEnumerable<string> arguments, KeyValuePair<string, string> credentials=default)
        {
            try
            {
                if (credentials.Value is not null)
                {

                    fixed(char* symbol = credentials.Value)
                    {
                        var info = new ProcessStartInfo() { WindowStyle = style, FileName = processExeFile, UserName = credentials.Key, Password = new SecureString(symbol, credentials.Value.Length) };
                        foreach (var arg in arguments)
                        {
                            info.ArgumentList.Add(arg);
                        }
                        Process.Start(info);
                    }
                }
                else {
                    var info = new ProcessStartInfo() { WindowStyle = style, FileName = processExeFile };
                    foreach (var arg in arguments)
                    {
                        info.ArgumentList.Add(arg);
                    }
                    Process.Start(info);
                }
            }
            catch
            {
                _printer.Print(_localizer["ProcessStartError"]);
            }
        }
        public async ValueTask KillProcess(string name, bool includeProcessTree)
        {
            try
            {
                Process.GetProcessesByName(name)[0]?.Kill(includeProcessTree);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessKillError"]);
            }
        }
        public async ValueTask CloseProcessMainWindow(int id)
        {
            try
            {
                Process.GetProcessById(id).CloseMainWindow();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessCloseWindowError"]);
            }
        }
        public async ValueTask CloseProcessMainWindow(string name)
        {
            try
            {
                Process.GetProcessesByName(name)[0].CloseMainWindow();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessCloseWindowError"]);
            }
        }
        public async ValueTask WatchProcesses()
        {
            try
            {
                foreach(var process in Process.GetProcesses())
                {
                    await _formatter.PrintProcessInfo(process);
                }
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessResolvingError"]);
            }
        }
        public async ValueTask AboutProcess(int id)
        {
            try
            {
                await _formatter.PrintProcessInfo(Process.GetProcessById(id));
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessResolvingError"]);
            }
        }
        public async ValueTask AboutProcess()
        {
            try
            {
                await _formatter.PrintProcessInfo(Process.GetCurrentProcess());
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessResolvingError"]);
            }
        }
        public async ValueTask AboutProcess(string name)
        {
            try
            {
                await _formatter.PrintProcessInfo(Process.GetProcessesByName(name)[0]);
            }
            catch
            {
                await _printer.PrintAsync(_localizer["ProcessResolvingError"]);
            }
        }
        public async ValueTask ExecuteInDebugMode<T>(Action<T> callback, T param)
        {
            try
            {
                Process.EnterDebugMode();
                callback.Invoke(param);
                Process.LeaveDebugMode();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["DebugModeError"]);
            }
        }
    }

}

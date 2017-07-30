using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;

namespace ClrMethodUsageLogger.ConsoleApp
{
    public sealed class MethodJittedMonitor : IObservable<MethodJitted>, IDisposable
    {
        readonly string[] _assemblyFilters;
        readonly string[] _namespaceFilters;
        readonly TraceEventSession _traceSession;
        readonly IDisposable _moduleNameObservable;
        readonly ConcurrentDictionary<long, string> _moduleNames = new ConcurrentDictionary<long, string>();
        readonly IObservable<MethodJitted> _methodJittedObservable;

        public MethodJittedMonitor(string[] assemblyFilters, string[] namespaceFilters)
        {
            _assemblyFilters = assemblyFilters;
            _namespaceFilters = namespaceFilters;

            _traceSession = new TraceEventSession(nameof(MethodJittedMonitor));
            _traceSession.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, (ulong)ClrTraceEventParser.Keywords.Default);

            var moduleLoadStream = _traceSession.Source.Clr.Observe<ModuleLoadUnloadTraceData>("Loader/ModuleLoad");
            _moduleNameObservable = moduleLoadStream.Subscribe(load => _moduleNames.TryAdd(load.ModuleID, load.ModuleILFileName));

            var jittingStartedStream = _traceSession.Source.Clr.Observe<MethodJittingStartedTraceData>("Method/JittingStarted");

            _methodJittedObservable = jittingStartedStream
                .Where(traceData => AssemblyMatchesFilter(traceData.ModuleID) && NamespaceMatchesFilter(traceData.MethodNamespace))
                .Select(traceData => new MethodJitted(
                    traceData.ProcessID,
                    traceData.MethodName,
                    traceData.MethodSignature,
                    traceData.MethodNamespace,
                    _moduleNames.TryGetValue(traceData.ModuleID, out string result) ? result : null));
        }

        public IDisposable Subscribe(IObserver<MethodJitted> observer) => _methodJittedObservable.Subscribe(observer);

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            var traceTask = Task.Run(() => _traceSession.Source.Process());

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }

            _traceSession.Stop();
            await traceTask;
        }

        public void Dispose()
        {
            _moduleNameObservable.Dispose();
            _traceSession.Dispose();
        }

        bool AssemblyMatchesFilter(long moduleId)
        {
            if (_assemblyFilters == null)
                return true; // no filter, so it matches

            if (!_moduleNames.TryGetValue(moduleId, out string moduleName))
                return true; // unknown module, better to pretend it matches the filter rather than discard

            foreach (var filter in _assemblyFilters)
            {
                if (moduleName.Contains(filter))
                    return true;
            }

            return false;
        }

        bool NamespaceMatchesFilter(string @namespace)
        {
            if (_namespaceFilters == null)
                return true; // no filter, so it matches

            foreach (var filter in _namespaceFilters)
            {
                if (@namespace.Contains(filter))
                    return true;
            }

            return false;
        }
    }
}

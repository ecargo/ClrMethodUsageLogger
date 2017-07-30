using System;
using System.Threading;
using System.Threading.Tasks;
using Fclp;
using System.Collections.Generic;
using System.Security.Principal;
using System.IO;

namespace ClrMethodUsageLogger.ConsoleApp
{
    class Program
    {
        static string _outputFile;

        static void Main(string[] args)
        {
            if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
            {
                throw new InvalidOperationException("Must be run as administrator.");
            }

            var (outputFile, assemblyFilters, namespaceFilters) = ParseArguments(args);
            _outputFile = outputFile;

            var cancellationTokenSource = new CancellationTokenSource();
            var task = Monitor(cancellationTokenSource.Token, assemblyFilters, namespaceFilters);

            Console.WriteLine("Ready to go!");
            Console.ReadLine();
            cancellationTokenSource.Cancel();
            task.Wait();
        }

        static async Task Monitor(CancellationToken cancellationToken, string[] assemblyFilters, string[] namespaceFilters)
        {
            var methodJittedMonitor = new MethodJittedMonitor(assemblyFilters, namespaceFilters);
            methodJittedMonitor.Subscribe(details => File.AppendAllText(_outputFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:ffff},{details.Module ?? "<unknown module>"},{details.MethodNamespace},{details.MethodName}\r\n"));

            await methodJittedMonitor.ProcessAsync(cancellationToken);
        }

        static (string outputFile, string[] assemblyFilters, string[] namespaceFilters) ParseArguments(string[] args)
        {
            string outputFile = null;
            string[] assemblyFilters = null;
            string[] namespaceFilters = null;

            var parser = new FluentCommandLineParser();
            parser.Setup<string>('o', "output").Callback(value => outputFile = value).Required();
            parser.Setup<List<string>>('a', "assemblyFilters").Callback(value => assemblyFilters = value.ToArray());
            parser.Setup<List<string>>('n', "namespaceFilters").Callback(value => namespaceFilters = value.ToArray());

            var results = parser.Parse(args);
            if (results.HasErrors)
            {
                throw new ArgumentException(results.ErrorText);
            }

            return (outputFile, assemblyFilters, namespaceFilters);
        }
    }
}

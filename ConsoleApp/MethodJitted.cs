namespace ClrMethodUsageLogger.ConsoleApp
{
    public class MethodJitted
    {
        public int ProcessId { get; }
        public string MethodName { get; }
        public string MethodSignature { get; }
        public string MethodNamespace { get; }
        public string Module { get; }

        public MethodJitted(int processId, string methodName, string methodSignature, string methodNamespace, string module)
        {
            ProcessId = processId;
            MethodName = methodName;
            MethodSignature = methodSignature;
            MethodNamespace = methodNamespace;
            Module = module;
        }
    }
}

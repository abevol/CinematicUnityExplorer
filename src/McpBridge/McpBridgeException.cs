namespace UnityExplorer.McpBridge
{
    internal class McpBridgeException : Exception
    {
        public string Code { get; }

        public McpBridgeException(string code, string message) : base(message)
        {
            Code = code;
        }
    }
}

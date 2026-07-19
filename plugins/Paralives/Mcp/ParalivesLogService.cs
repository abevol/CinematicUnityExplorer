#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesLogService
    {
        private const int MaxLogBufferSize = 1000;

        private static readonly List<LogEntry> logBuffer = new();
        private static readonly object logLock = new();
        private static readonly Dictionary<string, LogSubscription> subscriptions = new();
        private static bool isLogCallbackRegistered;

        public static readonly Dictionary<string, Func<Dictionary<string, object>, object>> Actions = new()
        {
            ["get_game_logs"] = GetGameLogs,
            ["subscribe_logs"] = SubscribeLogs,
            ["poll_logs"] = PollLogs
        };

        private class LogEntry
        {
            public int Id;
            public string Type;
            public string Message;
            public string StackTrace;
            public DateTime Timestamp;
            public int CollapseCount;
        }

        private class LogSubscription
        {
            public string Id;
            public HashSet<string> Types;
            public List<LogEntry> Buffer;
            public int MaxSize;
            public DateTime CreatedAt;
        }

        private static void EnsureLogCallbackRegistered()
        {
            if (isLogCallbackRegistered)
                return;

            Application.logMessageReceived += OnLogMessageReceived;
            isLogCallbackRegistered = true;
            ExplorerCore.Log("ParalivesLogService: Unity log callback registered.");
        }

        private static void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            lock (logLock)
            {
                string typeStr = type.ToString().ToLower();

                if (logBuffer.Count > 0)
                {
                    LogEntry lastEntry = logBuffer[logBuffer.Count - 1];
                    if (lastEntry.Message == message && lastEntry.Type == typeStr)
                    {
                        lastEntry.CollapseCount++;
                        PushToSubscribers(lastEntry, typeStr);
                        return;
                    }
                }

                LogEntry newEntry = new()
                {
                    Id = logBuffer.Count + 1,
                    Type = typeStr,
                    Message = message,
                    StackTrace = stackTrace,
                    Timestamp = DateTime.UtcNow,
                    CollapseCount = 1
                };

                logBuffer.Add(newEntry);
                while (logBuffer.Count > MaxLogBufferSize)
                    logBuffer.RemoveAt(0);

                PushToSubscribers(newEntry, typeStr);
            }
        }

        private static void PushToSubscribers(LogEntry entry, string typeStr)
        {
            foreach (LogSubscription subscription in subscriptions.Values)
            {
                if (!subscription.Types.Contains(typeStr))
                    continue;

                subscription.Buffer.Add(entry);
                while (subscription.Buffer.Count > subscription.MaxSize)
                    subscription.Buffer.RemoveAt(0);
            }
        }

        private static object GetGameLogs(Dictionary<string, object> parameters)
        {
            EnsureLogCallbackRegistered();

            int limit = McpParameters.OptionalInt(parameters, "limit", 50);
            string type = McpParameters.OptionalString(parameters, "type") ?? "all";
            bool includeCollapsed = McpParameters.OptionalBool(parameters, "includeCollapsed", true);

            List<object> logs = new();
            int logCount = 0;
            int warningCount = 0;
            int exceptionCount = 0;

            lock (logLock)
            {
                IEnumerable<LogEntry> filteredLogs = type == "all"
                    ? logBuffer
                    : logBuffer.Where(l => l.Type == type);

                List<LogEntry> logsList = new(filteredLogs);
                int startIndex = Math.Max(0, logsList.Count - limit);
                for (int i = startIndex; i < logsList.Count; i++)
                {
                    LogEntry entry = logsList[i];
                    logs.Add(new Dictionary<string, object>
                    {
                        ["id"] = entry.Id,
                        ["type"] = entry.Type,
                        ["message"] = entry.Message,
                        ["timestamp"] = entry.Timestamp.ToString("O"),
                        ["collapseCount"] = includeCollapsed ? entry.CollapseCount : 1,
                        ["stackTrace"] = string.IsNullOrEmpty(entry.StackTrace) ? null : entry.StackTrace
                    });
                }

                logCount = logBuffer.Count(l => l.Type == "log");
                warningCount = logBuffer.Count(l => l.Type == "warning");
                exceptionCount = logBuffer.Count(l => l.Type == "exception");
            }

            return new Dictionary<string, object>
            {
                ["logs"] = logs,
                ["totalCount"] = logCount + warningCount + exceptionCount,
                ["logCount"] = logCount,
                ["warningCount"] = warningCount,
                ["exceptionCount"] = exceptionCount,
                ["limit"] = limit,
                ["type"] = type
            };
        }

        private static object SubscribeLogs(Dictionary<string, object> parameters)
        {
            EnsureLogCallbackRegistered();

            int bufferSize = McpParameters.OptionalInt(parameters, "bufferSize", 100);
            List<object> typesArray = McpParameters.OptionalArray(parameters, "types");
            HashSet<string> types = new(StringComparer.OrdinalIgnoreCase);

            if (typesArray.Count > 0)
            {
                foreach (object typeObj in typesArray)
                {
                    if (typeObj != null)
                        types.Add(typeObj.ToString().ToLower());
                }
            }
            else
            {
                types.Add("log");
                types.Add("warning");
                types.Add("exception");
            }

            string subscriptionId = $"sub_{Guid.NewGuid():N}";

            lock (logLock)
            {
                subscriptions[subscriptionId] = new LogSubscription
                {
                    Id = subscriptionId,
                    Types = types,
                    Buffer = new List<LogEntry>(),
                    MaxSize = bufferSize,
                    CreatedAt = DateTime.UtcNow
                };
            }

            return new Dictionary<string, object>
            {
                ["subscriptionId"] = subscriptionId,
                ["status"] = "active",
                ["bufferSize"] = bufferSize,
                ["subscribedTypes"] = types.ToList()
            };
        }

        private static object PollLogs(Dictionary<string, object> parameters)
        {
            string subscriptionId = McpParameters.RequiredString(parameters, "subscriptionId");
            int limit = McpParameters.OptionalInt(parameters, "limit", 50);

            LogSubscription subscription;
            lock (logLock)
            {
                if (!subscriptions.TryGetValue(subscriptionId, out subscription))
                    throw new McpBridgeException("not_found", $"Subscription '{subscriptionId}' not found.");
            }

            List<object> logs = new();
            bool hasMore = false;

            lock (logLock)
            {
                List<LogEntry> bufferCopy = subscription.Buffer.ToList();
                subscription.Buffer.Clear();

                foreach (LogEntry entry in bufferCopy.Take(limit))
                {
                    logs.Add(new Dictionary<string, object>
                    {
                        ["id"] = entry.Id,
                        ["type"] = entry.Type,
                        ["message"] = entry.Message,
                        ["timestamp"] = entry.Timestamp.ToString("O"),
                        ["collapseCount"] = entry.CollapseCount,
                        ["stackTrace"] = string.IsNullOrEmpty(entry.StackTrace) ? null : entry.StackTrace
                    });
                }

                hasMore = bufferCopy.Count > limit;
            }

            return new Dictionary<string, object>
            {
                ["logs"] = logs,
                ["hasMore"] = hasMore,
                ["nextPollToken"] = DateTime.UtcNow.Ticks.ToString()
            };
        }
    }
}
#endif

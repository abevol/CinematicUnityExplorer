#if MONO
using System.Diagnostics;
using System.Globalization;

namespace UnityExplorer.McpBridge.Paralives
{
    /// <summary>
    /// Compatibility facade. Public actions are retained, but the data exposed here is
    /// lightweight runtime counters unless an explicit Unity profiler action is called.
    /// </summary>
    internal static class ParalivesProfilerService
    {
        private const int MaxFpsSamples = 60;
        private const float FpsUpdateInterval = 0.1f;
        private const double SceneCacheTtlMs = 10000;

        private static readonly Queue<float> fpsSamples = new();
        private static readonly Dictionary<string, object> recorders = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, long> latestRecorderValues = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> warmingRecorders = new(StringComparer.OrdinalIgnoreCase);

        private static float fpsAccumulator;
        private static int fpsFrameCount;
        private static float fpsCurrent;
        private static float fpsTimer;
        private static long lastManagedHeapBytes;
        private static long managedHeapDeltaBytes;

        private static int cachedGameObjectCount;
        private static int cachedComponentCount;
        private static DateTime? sceneCacheUpdatedUtc;
        private static long sceneRefreshDurationMs;

        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> actionHandlers = new()
        {
            ["paralives_get_performance_stats"] = _ => GetPerformanceStats(),
            ["paralives_get_performance_history"] = GetPerformanceHistory,
            ["paralives_get_memory_stats"] = _ => GetMemoryStats(),
            ["paralives_get_scene_stats"] = GetSceneStats,
            ["paralives_get_frame_timing"] = _ => GetFrameTiming(),
            ["paralives_list_profiler_counters"] = ListProfilerCounters,
            ["paralives_get_profiler_counter_samples"] = GetProfilerCounterSamples
        };

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            if (actionHandlers.TryGetValue(action, out Func<Dictionary<string, object>, object> handler))
                return handler(parameters);

            throw new McpBridgeException("invalid_request", $"Unknown performance action '{action}'.");
        }

        public static void Update()
        {
            float deltaTime = UnityEngine.Time.unscaledDeltaTime;
            fpsAccumulator += deltaTime;
            fpsFrameCount++;
            fpsTimer += deltaTime;

            if (fpsTimer >= FpsUpdateInterval)
            {
                fpsCurrent = fpsFrameCount / fpsAccumulator;
                fpsSamples.Enqueue(fpsCurrent);
                while (fpsSamples.Count > MaxFpsSamples)
                    fpsSamples.Dequeue();

                fpsAccumulator = 0;
                fpsFrameCount = 0;
                fpsTimer = 0;
            }

            UpdateProfilerRecorderSamples();
        }

        public static void Shutdown()
        {
            foreach (object recorder in recorders.Values)
                DisposeRecorder(recorder);

            recorders.Clear();
            latestRecorderValues.Clear();
            warmingRecorders.Clear();
        }

        private static object GetPerformanceStats()
        {
            float[] samples = fpsSamples.ToArray();
            float avgFps = samples.Length > 0 ? samples.Average() : 0;
            float minFps = samples.Length > 0 ? samples.Min() : 0;
            float maxFps = samples.Length > 0 ? samples.Max() : 0;

            long totalMemory = GC.GetTotalMemory(false);
            if (lastManagedHeapBytes == 0)
                lastManagedHeapBytes = totalMemory;
            managedHeapDeltaBytes = totalMemory - lastManagedHeapBytes;
            lastManagedHeapBytes = totalMemory;

            int gcGen0 = GC.CollectionCount(0);
            int gcGen1 = GC.CollectionCount(1);
            int gcGen2 = GC.CollectionCount(2);
            Dictionary<string, object> scene = BuildSceneCacheSummary();

            return new Dictionary<string, object>
            {
                ["kind"] = "performance_counters",
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["fps"] = new Dictionary<string, object>
                {
                    ["current"] = Math.Round(fpsCurrent, 1),
                    ["average"] = Math.Round(avgFps, 1),
                    ["min"] = Math.Round(minFps, 1),
                    ["max"] = Math.Round(maxFps, 1),
                    ["sampleCount"] = samples.Length
                },
                ["memory"] = new Dictionary<string, object>
                {
                    ["managedHeap"] = FormatBytes(totalMemory),
                    ["managedHeapBytes"] = totalMemory,
                    ["managedHeapDelta"] = FormatBytes(managedHeapDeltaBytes),
                    ["managedHeapDeltaBytes"] = managedHeapDeltaBytes,
                    ["gcAllocDelta"] = FormatBytes(managedHeapDeltaBytes),
                    ["gcAllocDeltaBytes"] = managedHeapDeltaBytes,
                    ["gcAllocDeltaDeprecated"] = true
                },
                ["gc"] = new Dictionary<string, object>
                {
                    ["gen0Collections"] = gcGen0,
                    ["gen1Collections"] = gcGen1,
                    ["gen2Collections"] = gcGen2,
                    ["totalCollections"] = gcGen0 + gcGen1 + gcGen2
                },
                ["scene"] = scene,
                ["time"] = new Dictionary<string, object>
                {
                    ["timeScale"] = UnityEngine.Time.timeScale,
                    ["realtimeSinceStartup"] = Math.Round(UnityEngine.Time.realtimeSinceStartup, 2),
                    ["timeSinceLevelLoad"] = Math.Round(UnityEngine.Time.timeSinceLevelLoad, 2)
                },
                ["note"] = "Lightweight counters only. Use Paralives:get_frame_timing and profiler counter tools for Unity profiler data."
            };
        }

        private static object GetPerformanceHistory(Dictionary<string, object> parameters)
        {
            int limit = Math.Min(McpParameters.OptionalInt(parameters, "limit", 50), MaxFpsSamples);
            List<object> samples = new();
            float[] fpsArray = fpsSamples.ToArray();
            int startIndex = Math.Max(0, fpsArray.Length - limit);

            for (int i = startIndex; i < fpsArray.Length; i++)
                samples.Add(new Dictionary<string, object> { ["fps"] = Math.Round(fpsArray[i], 1) });

            return new Dictionary<string, object>
            {
                ["kind"] = "performance_counters",
                ["samples"] = samples,
                ["count"] = samples.Count,
                ["limit"] = limit,
                ["interval"] = FpsUpdateInterval
            };
        }

        private static object GetMemoryStats()
        {
            long totalMemory = GC.GetTotalMemory(false);
            return new Dictionary<string, object>
            {
                ["kind"] = "performance_counters",
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["managedHeap"] = new Dictionary<string, object>
                {
                    ["total"] = FormatBytes(totalMemory),
                    ["totalBytes"] = totalMemory
                },
                ["gc"] = new Dictionary<string, object>
                {
                    ["gen0Collections"] = GC.CollectionCount(0),
                    ["gen1Collections"] = GC.CollectionCount(1),
                    ["gen2Collections"] = GC.CollectionCount(2)
                }
            };
        }

        private static object GetSceneStats(Dictionary<string, object> parameters)
        {
            bool forceRefresh = McpParameters.OptionalBool(parameters, "forceRefresh", false);
            EnsureSceneStats(forceRefresh);

            Dictionary<string, object> result = BuildSceneCacheSummary();
            result["timestamp"] = DateTime.UtcNow.ToString("O");
            result["loadedScenes"] = UnityEngine.SceneManagement.SceneManager.sceneCount;
            result["rootObjects"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount;
            result["forceRefresh"] = forceRefresh;
            return result;
        }

        private static void EnsureSceneStats(bool forceRefresh)
        {
            if (!forceRefresh && sceneCacheUpdatedUtc.HasValue && GetSceneCacheAgeMs() <= SceneCacheTtlMs)
                return;

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                cachedGameObjectCount = RuntimeHelper.FindObjectsOfTypeAll(typeof(UnityEngine.GameObject)).Length;
                cachedComponentCount = RuntimeHelper.FindObjectsOfTypeAll(typeof(UnityEngine.Component)).Length;
                sceneCacheUpdatedUtc = DateTime.UtcNow;
            }
            catch
            {
            }
            finally
            {
                sw.Stop();
                sceneRefreshDurationMs = sw.ElapsedMilliseconds;
            }
        }

        private static Dictionary<string, object> BuildSceneCacheSummary()
        {
            double ageMs = GetSceneCacheAgeMs();
            return new Dictionary<string, object>
            {
                ["gameObjects"] = cachedGameObjectCount,
                ["components"] = cachedComponentCount,
                ["activeScene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                ["cacheAgeMs"] = sceneCacheUpdatedUtc.HasValue ? Math.Round(ageMs, 0) : null,
                ["cacheStale"] = !sceneCacheUpdatedUtc.HasValue || ageMs > SceneCacheTtlMs,
                ["lastUpdatedUtc"] = sceneCacheUpdatedUtc.HasValue ? sceneCacheUpdatedUtc.Value.ToString("O") : null,
                ["refreshDurationMs"] = sceneRefreshDurationMs
            };
        }

        private static double GetSceneCacheAgeMs()
        {
            if (!sceneCacheUpdatedUtc.HasValue)
                return double.PositiveInfinity;
            return (DateTime.UtcNow - sceneCacheUpdatedUtc.Value).TotalMilliseconds;
        }

        private static object GetFrameTiming()
        {
            try
            {
                Type managerType = ResolveUnityType("UnityEngine.FrameTimingManager");
                Type timingType = ResolveUnityType("UnityEngine.FrameTiming");
                if (managerType == null || timingType == null)
                    return Unsupported("FrameTimingManager is not available in this Unity runtime.");

                managerType.GetMethod("CaptureFrameTimings", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                Array timingArray = Array.CreateInstance(timingType, 1);
                MethodInfo getLatest = managerType.GetMethod("GetLatestTimings", BindingFlags.Public | BindingFlags.Static);
                if (getLatest == null)
                    return Unsupported("FrameTimingManager.GetLatestTimings is not available.");

                object sampleCountObj = getLatest.Invoke(null, new object[] { (uint)1, timingArray });
                int sampleCount = Convert.ToInt32(sampleCountObj, CultureInfo.InvariantCulture);
                if (sampleCount <= 0)
                    return new Dictionary<string, object> { ["supported"] = true, ["sampleCount"] = 0, ["warmingUp"] = true };

                object timing = timingArray.GetValue(0);
                return new Dictionary<string, object>
                {
                    ["supported"] = true,
                    ["sampleCount"] = sampleCount,
                    ["cpuFrameTimeMs"] = ReadDouble(timing, timingType, "cpuFrameTime"),
                    ["gpuFrameTimeMs"] = ReadDouble(timing, timingType, "gpuFrameTime"),
                    ["mainThreadTimeMs"] = ReadDouble(timing, timingType, "cpuMainThreadFrameTime"),
                    ["renderThreadTimeMs"] = ReadDouble(timing, timingType, "cpuRenderThreadFrameTime")
                };
            }
            catch (Exception ex)
            {
                return Unsupported(ex.GetInnerMostException().Message);
            }
        }

        private static object ListProfilerCounters(Dictionary<string, object> parameters)
        {
            string query = McpParameters.OptionalString(parameters, "query");
            string categoryFilter = McpParameters.OptionalString(parameters, "category");
            int limit = McpParameters.Clamp(McpParameters.OptionalInt(parameters, "limit", 100), 1, 500);

            Type handleType = ResolveUnityType("UnityEngine.Profiling.ProfilerRecorderHandle");
            if (handleType == null)
                return Unsupported("ProfilerRecorderHandle is not available in this Unity runtime.");

            object list = Activator.CreateInstance(typeof(List<>).MakeGenericType(handleType));
            MethodInfo getAvailable = handleType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "GetAvailable" && method.GetParameters().Length == 1);
            if (getAvailable == null)
                return Unsupported("ProfilerRecorderHandle.GetAvailable is not available.");

            getAvailable.Invoke(null, new[] { list });
            List<object> counters = new();
            foreach (object handle in (System.Collections.IEnumerable)list)
            {
                string name = ReadMemberAsString(handle, handleType, "Name");
                string category = ReadMemberAsString(handle, handleType, "Category");
                string unitType = ReadMemberAsString(handle, handleType, "UnitType");

                if (!string.IsNullOrEmpty(query) && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (!string.IsNullOrEmpty(categoryFilter) && category.IndexOf(categoryFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                counters.Add(new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["category"] = category,
                    ["unitType"] = unitType
                });

                if (counters.Count >= limit)
                    break;
            }

            return new Dictionary<string, object>
            {
                ["supported"] = true,
                ["counters"] = counters,
                ["limit"] = limit,
                ["truncated"] = counters.Count >= limit
            };
        }

        private static object GetProfilerCounterSamples(Dictionary<string, object> parameters)
        {
            List<object> counters = McpParameters.OptionalArray(parameters, "counters");
            if (counters.Count == 0)
                throw new McpBridgeException("invalid_request", "'counters' must contain at least one profiler counter name.");

            List<object> samples = new();
            foreach (object counterObj in counters)
            {
                string category = null;
                string counter = ParseCounterRequest(counterObj, out category);
                if (string.IsNullOrEmpty(counter))
                    continue;

                string key = BuildRecorderKey(category, counter);
                EnsureRecorder(category, counter);
                bool warmingUp = warmingRecorders.Contains(key) || !latestRecorderValues.ContainsKey(key);
                samples.Add(new Dictionary<string, object>
                {
                    ["name"] = counter,
                    ["category"] = category,
                    ["value"] = warmingUp ? null : latestRecorderValues[key],
                    ["warmingUp"] = warmingUp,
                    ["supported"] = recorders.ContainsKey(key)
                });
            }

            return new Dictionary<string, object>
            {
                ["supported"] = true,
                ["samples"] = samples,
                ["sampleCount"] = samples.Count
            };
        }

        private static string ParseCounterRequest(object counterObj, out string category)
        {
            category = null;
            if (counterObj is Dictionary<string, object> dict)
            {
                string name = dict.TryGetValue("name", out object nameValue) ? nameValue?.ToString() : null;
                category = dict.TryGetValue("category", out object categoryValue) ? categoryValue?.ToString() : null;
                return name;
            }

            string text = counterObj?.ToString();
            if (string.IsNullOrEmpty(text))
                return null;

            int separator = text.IndexOf('/');
            if (separator < 0)
                separator = text.IndexOf(':');

            if (separator > 0 && separator < text.Length - 1)
            {
                category = text.Substring(0, separator).Trim();
                return text.Substring(separator + 1).Trim();
            }

            return text;
        }

        private static void EnsureRecorder(string categoryName, string counter)
        {
            string key = BuildRecorderKey(categoryName, counter);
            if (recorders.ContainsKey(key))
                return;

            try
            {
                Type recorderType = ResolveUnityType("UnityEngine.Profiling.ProfilerRecorder");
                Type categoryType = ResolveUnityType("UnityEngine.Profiling.ProfilerCategory");
                if (recorderType == null || categoryType == null)
                    return;

                object category = ResolveProfilerCategory(categoryType, categoryName)
                    ?? ReadStaticMember(categoryType, "Scripts")
                    ?? ReadStaticMember(categoryType, "Internal");
                MethodInfo startNew = recorderType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method =>
                    {
                        if (method.Name != "StartNew")
                            return false;
                        ParameterInfo[] args = method.GetParameters();
                        return args.Length >= 2 && args[0].ParameterType == categoryType && args[1].ParameterType == typeof(string);
                    });
                if (startNew == null || category == null)
                    return;

                ParameterInfo[] parameters = startNew.GetParameters();
                object[] args = new object[parameters.Length];
                args[0] = category;
                args[1] = counter;
                for (int i = 2; i < args.Length; i++)
                    args[i] = GetRecorderStartDefault(parameters[i].ParameterType);

                object recorder = startNew.Invoke(null, args);
                recorders[key] = recorder;
                warmingRecorders.Add(key);
            }
            catch
            {
            }
        }

        private static void UpdateProfilerRecorderSamples()
        {
            foreach (KeyValuePair<string, object> pair in recorders.ToList())
            {
                try
                {
                    Type type = pair.Value.GetType();
                    object value = UnityReflectionUtility.ReadMember(pair.Value, type, "LastValue");
                    latestRecorderValues[pair.Key] = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                    warmingRecorders.Remove(pair.Key);
                }
                catch
                {
                    warmingRecorders.Add(pair.Key);
                }
            }
        }

        private static string BuildRecorderKey(string categoryName, string counter)
        {
            return $"{categoryName ?? "Scripts"}:{counter}";
        }

        private static object ResolveProfilerCategory(Type categoryType, string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return null;

            string normalized = new string(categoryName.Where(char.IsLetterOrDigit).ToArray());
            return ReadStaticMember(categoryType, categoryName)
                ?? ReadStaticMember(categoryType, normalized);
        }

        private static object GetRecorderStartDefault(Type parameterType)
        {
            if (parameterType == typeof(int))
                return 1;
            if (parameterType == typeof(uint))
                return (uint)1;
            if (parameterType == typeof(bool))
                return false;
            if (parameterType.IsValueType)
                return Activator.CreateInstance(parameterType);
            return null;
        }

        private static void DisposeRecorder(object recorder)
        {
            try
            {
                recorder.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)?.Invoke(recorder, null);
            }
            catch
            {
            }
        }

        private static Type ResolveUnityType(string fullName)
        {
            return Type.GetType(fullName + ", UnityEngine.CoreModule")
                ?? Type.GetType(fullName + ", UnityEngine")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType(fullName))
                    .FirstOrDefault(type => type != null);
        }

        private static object ReadStaticMember(Type type, string memberName)
        {
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
            if (property != null)
                return property.GetValue(null, null);

            FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            return field != null ? field.GetValue(null) : null;
        }

        private static double ReadDouble(object owner, Type type, string memberName)
        {
            if (!UnityReflectionUtility.TryReadMember(owner, type, memberName, out object value) || value == null)
                return 0;
            return Math.Round(Convert.ToDouble(value, CultureInfo.InvariantCulture), 3);
        }

        private static string ReadMemberAsString(object owner, Type type, string memberName)
        {
            return UnityReflectionUtility.TryReadMember(owner, type, memberName, out object value) && value != null
                ? value.ToString()
                : "";
        }

        private static Dictionary<string, object> Unsupported(string error)
        {
            return new Dictionary<string, object>
            {
                ["supported"] = false,
                ["error"] = error
            };
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;

            while (Math.Abs(dblBytes) >= 1024 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }

            return $"{dblBytes.ToString("0.##", CultureInfo.InvariantCulture)} {suffixes[i]}";
        }
    }
}
#endif

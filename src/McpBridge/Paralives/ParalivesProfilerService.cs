#if MONO
using System.Diagnostics;
using System.Globalization;

namespace UnityExplorer.McpBridge.Paralives
{
    /// <summary>
    /// 运行时性能分析服务
    /// </summary>
    internal static class ParalivesProfilerService
    {
        // FPS 采样
        private static readonly Queue<float> fpsSamples = new();
        private static readonly int maxFpsSamples = 60; // 保留 60 个采样点
        private static float fpsUpdateInterval = 0.1f; // 每 100ms 更新一次
        private static float fpsAccumulator = 0;
        private static int fpsFrameCount = 0;
        private static float fpsCurrent = 0;
        private static float fpsTimer = 0;

        // 内存采样
        private static long lastGcMemory = 0;
        private static long gcAllocDelta = 0;

        // 场景统计缓存
        private static int cachedGameObjectCount = 0;
        private static int cachedComponentCount = 0;
        private static float sceneStatsTimer = 0;
        private static float sceneStatsInterval = 1.0f; // 每秒更新一次

        /// <summary>
        /// 处理性能分析相关的 action
        /// </summary>
        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            return action switch
            {
                "paralives_get_performance_stats" => GetPerformanceStats(),
                "paralives_get_performance_history" => GetPerformanceHistory(parameters),
                "paralives_get_memory_stats" => GetMemoryStats(),
                "paralives_get_scene_stats" => GetSceneStats(),
                _ => throw new McpBridgeException("invalid_request", $"Unknown profiler action '{action}'.")
            };
        }

        /// <summary>
        /// 更新性能数据（每帧调用）
        /// </summary>
        public static void Update()
        {
            // 更新 FPS
            float deltaTime = UnityEngine.Time.unscaledDeltaTime;
            fpsAccumulator += deltaTime;
            fpsFrameCount++;
            fpsTimer += deltaTime;

            if (fpsTimer >= fpsUpdateInterval)
            {
                fpsCurrent = fpsFrameCount / fpsAccumulator;
                fpsSamples.Enqueue(fpsCurrent);
                while (fpsSamples.Count > maxFpsSamples)
                    fpsSamples.Dequeue();

                fpsAccumulator = 0;
                fpsFrameCount = 0;
                fpsTimer = 0;
            }

            // 更新场景统计
            sceneStatsTimer += deltaTime;
            if (sceneStatsTimer >= sceneStatsInterval)
            {
                UpdateSceneStats();
                sceneStatsTimer = 0;
            }
        }

        /// <summary>
        /// 获取综合性能统计
        /// </summary>
        private static object GetPerformanceStats()
        {
            // FPS 统计
            float avgFps = fpsSamples.Count > 0 ? fpsSamples.ToArray().Average() : 0;
            float minFps = fpsSamples.Count > 0 ? fpsSamples.ToArray().Min() : 0;
            float maxFps = fpsSamples.Count > 0 ? fpsSamples.ToArray().Max() : 0;

            // 内存统计（使用 GC API，不依赖 UnityEngine.Profiling）
            long totalMemory = GC.GetTotalMemory(false);

            // GC 分配计算
            if (lastGcMemory == 0)
                lastGcMemory = totalMemory;
            gcAllocDelta = totalMemory - lastGcMemory;
            lastGcMemory = totalMemory;

            // GC 收集次数
            int gcGen0 = GC.CollectionCount(0);
            int gcGen1 = GC.CollectionCount(1);
            int gcGen2 = GC.CollectionCount(2);

            // 游戏对象统计
            UpdateSceneStats();

            return new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["fps"] = new Dictionary<string, object>
                {
                    ["current"] = Math.Round(fpsCurrent, 1),
                    ["average"] = Math.Round(avgFps, 1),
                    ["min"] = Math.Round(minFps, 1),
                    ["max"] = Math.Round(maxFps, 1),
                    ["sampleCount"] = fpsSamples.Count
                },
                ["memory"] = new Dictionary<string, object>
                {
                    ["managedHeap"] = FormatBytes(totalMemory),
                    ["managedHeapBytes"] = totalMemory,
                    ["gcAllocDelta"] = FormatBytes(gcAllocDelta),
                    ["gcAllocDeltaBytes"] = gcAllocDelta
                },
                ["gc"] = new Dictionary<string, object>
                {
                    ["gen0Collections"] = gcGen0,
                    ["gen1Collections"] = gcGen1,
                    ["gen2Collections"] = gcGen2,
                    ["totalCollections"] = gcGen0 + gcGen1 + gcGen2
                },
                ["scene"] = new Dictionary<string, object>
                {
                    ["gameObjects"] = cachedGameObjectCount,
                    ["components"] = cachedComponentCount,
                    ["activeScene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                },
                ["time"] = new Dictionary<string, object>
                {
                    ["timeScale"] = UnityEngine.Time.timeScale,
                    ["realtimeSinceStartup"] = Math.Round(UnityEngine.Time.realtimeSinceStartup, 2),
                    ["timeSinceLevelLoad"] = Math.Round(UnityEngine.Time.timeSinceLevelLoad, 2)
                }
            };
        }

        /// <summary>
        /// 获取性能历史（简化版，无历史记录）
        /// </summary>
        private static object GetPerformanceHistory(Dictionary<string, object> parameters)
        {
            int limit = GetOptionalInt(parameters, "limit", 50);
            limit = Math.Min(limit, maxFpsSamples);

            // 返回当前 FPS 采样
            List<object> samples = new();
            float[] fpsArray = fpsSamples.ToArray();
            int startIndex = Math.Max(0, fpsArray.Length - limit);
            
            for (int i = startIndex; i < fpsArray.Length; i++)
            {
                samples.Add(new Dictionary<string, object>
                {
                    ["fps"] = Math.Round(fpsArray[i], 1)
                });
            }

            return new Dictionary<string, object>
            {
                ["samples"] = samples,
                ["count"] = samples.Count,
                ["limit"] = limit,
                ["interval"] = fpsUpdateInterval
            };
        }

        /// <summary>
        /// 获取内存统计
        /// </summary>
        private static object GetMemoryStats()
        {
            long totalMemory = GC.GetTotalMemory(false);

            return new Dictionary<string, object>
            {
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
                },
                ["note"] = "UnityEngine.Profiling not available in Release build"
            };
        }

        /// <summary>
        /// 获取场景统计
        /// </summary>
        private static object GetSceneStats()
        {
            UpdateSceneStats();

            return new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["gameObjects"] = cachedGameObjectCount,
                ["components"] = cachedComponentCount,
                ["activeScene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                ["loadedScenes"] = UnityEngine.SceneManagement.SceneManager.sceneCount,
                ["rootObjects"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount
            };
        }

        /// <summary>
        /// 更新场景统计
        /// </summary>
        private static void UpdateSceneStats()
        {
            try
            {
                cachedGameObjectCount = RuntimeHelper.FindObjectsOfTypeAll(typeof(UnityEngine.GameObject)).Length;
                cachedComponentCount = RuntimeHelper.FindObjectsOfTypeAll(typeof(UnityEngine.Component)).Length;
            }
            catch
            {
                // 忽略错误
            }
        }

        /// <summary>
        /// 格式化字节数
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;

            while (dblBytes >= 1024 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }

            return $"{dblBytes.ToString("0.##", CultureInfo.InvariantCulture)} {suffixes[i]}";
        }

        /// <summary>
        /// 获取可选整数参数
        /// </summary>
        private static int GetOptionalInt(Dictionary<string, object> parameters, string name, int fallback)
        {
            return parameters.TryGetValue(name, out object value) && value != null
                ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : fallback;
        }
    }
}
#endif

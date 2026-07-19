#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesProfilerService
    {
        public static Dictionary<string, Func<Dictionary<string, object>, object>> Actions => ParalivesPerformanceCountersService.Actions;

        public static object Handle(string action, Dictionary<string, object> parameters)
        {
            return ParalivesPerformanceCountersService.Handle(action, parameters);
        }

        public static void Update()
        {
            ParalivesPerformanceCountersService.Update();
        }

        public static void Shutdown()
        {
            ParalivesPerformanceCountersService.Shutdown();
        }
    }
}
#endif

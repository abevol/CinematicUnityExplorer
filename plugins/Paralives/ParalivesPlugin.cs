using CinematicUnityExplorer.Plugins.Paralives.Mcp;
using CinematicUnityExplorer.Plugins.Paralives.UI;
using UnityExplorer.Plugins;

namespace CinematicUnityExplorer.Plugins.Paralives
{
    public sealed class ParalivesPlugin : IUnityExplorerPlugin
    {
        public string Id => "cinematic-unity-explorer.paralives";
        public string Name => "Paralives";
        public string Version => "1.0.0";

        public bool IsAvailable(IUnityExplorerPluginContext context)
            => ParalivesControlService.IsAvailable;

        public void Initialize(IUnityExplorerPluginContext context)
        {
            ParalivesPluginConfig.Register(context.Config, Id);
            ParalivesMcpRegistration.Register(context.Mcp);
            context.Panels.RegisterPanel(ParalivesPanel.CreateDescriptor());
        }

        public void Update()
            => ParalivesPerformanceCountersService.Update();

        public void Shutdown()
            => ParalivesPerformanceCountersService.Shutdown();
    }
}

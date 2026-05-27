using UnityExplorer.UI.Panels;

namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class WorldInspector : MouseInspectorBase
    {
        internal static Camera MainCamera;
        private static GameObject lastHitObject;

        public override void OnBeginMouseInspect()
        {
MainCamera = Camera.main;
            List<Camera> cameras = Camera.allCameras.ToList();

            if (!EnsureMainCamera())
            {
                ExplorerCore.LogWarning("No Main Camera was found, trying to find a camera named 'Main Camera' or 'MainCamera'.");
                MainCamera = cameras.FirstOrDefault(c => c.name == "Main Camera" || c.name == "MainCamera");

                if (!MainCamera)
                {
                    ExplorerCore.LogWarning("No camera named 'Main Camera' or 'MainCamera' found, using the first camera created.");
                    MainCamera = cameras.FirstOrDefault();
                    if (!MainCamera)
                    {
                        ExplorerCore.LogWarning("No valid cameras found! Cannot inspect world!");
                        return;
                    }
                }
            }

            MouseInspector.Instance.inspectorLabelTitle.text = $"<b>World Inspector ({MainCamera.name})</b> (press <b>ESC</b> to cancel)";
            ExplorerCore.Log($"Using camera: '{MainCamera.transform.GetTransformPath(true)}'");
        }

        /// <summary>
        /// Assigns it as the MainCamera and updates the inspector title.
        /// </summary>
        /// <param name="cam">The camera to assign.</param>
        private static void AssignCamAndUpdateTitle(Camera cam)
        {
            MainCamera = cam;
            MouseInspector.Instance.UpdateInspectorTitle(
                $"<b>World Inspector ({MainCamera.name})</b> (press <b>ESC</b> to cancel)"
            );
        }

        public override void ClearHitData()
        {
            lastHitObject = null;
        }

        public override void OnSelectMouseInspect(Action<GameObject> inspectorAction)
        {
            inspectorAction(lastHitObject);
        }

        /// <summary>
        /// Attempts to ensure that MainCamera is assigned. If not then attempts to find it.
        /// If no cameras are available, logs a warning and returns null.
        /// </summary>
        private static Camera EnsureMainCamera()
        {
            if (MainCamera){
                // We still call this in case the last title was from the UIInspector
                AssignCamAndUpdateTitle(MainCamera);
                return MainCamera;
            }

            if (Camera.main){
                AssignCamAndUpdateTitle(Camera.main);
                return MainCamera;
            }

            ExplorerCore.LogWarning("No Camera.main found, trying to find a camera named 'Main Camera' or 'MainCamera'...");
            Camera namedCam = Camera.allCameras.FirstOrDefault(c => c.name is "Main Camera" or "MainCamera"); 
            if (namedCam) {
                AssignCamAndUpdateTitle(namedCam);
                return MainCamera;
            }

            if (FreeCamPanel.inFreeCamMode && FreeCamPanel.GetFreecam()){
                AssignCamAndUpdateTitle(FreeCamPanel.GetFreecam());
                return MainCamera;
            }

            ExplorerCore.LogWarning("No camera named found, using the first camera created...");
            var fallbackCam = Camera.allCameras.FirstOrDefault();
            if (fallbackCam) {
                AssignCamAndUpdateTitle(fallbackCam);
                return MainCamera;
            }

            // If we reach here, no cameras were found at all.
            ExplorerCore.LogWarning("No valid cameras found!");
            return null;
        }

        public override void UpdateMouseInspect(Vector2 mousePos)
        {
            // Attempt to ensure camera each time UpdateMouseInspect is called
            // in case something changed or wasn't set initially.
            if (!EnsureMainCamera())
            {
                ExplorerCore.LogWarning("No Main Camera was found, unable to inspect world!");
                MouseInspector.Instance.StopInspect();
                return;
            }

            Ray ray = MainCamera.ScreenPointToRay(mousePos);
            Physics.Raycast(ray, out RaycastHit hit, 1000f);

            if (hit.transform)
                OnHitGameObject(hit.transform.gameObject);
            else if (lastHitObject)
                MouseInspector.Instance.ClearHitData();
        }

        internal void OnHitGameObject(GameObject obj)
        {
            if (obj != lastHitObject)
            {
                lastHitObject = obj;
                MouseInspector.Instance.UpdateObjectNameLabel($"<b>Click to Inspect:</b> <color=cyan>{obj.name}</color>");
                MouseInspector.Instance.UpdateObjectPathLabel($"Path: {obj.transform.GetTransformPath(true)}");
            }
        }

        public override void OnEndInspect()
        {
            // not needed
        }
    }
}

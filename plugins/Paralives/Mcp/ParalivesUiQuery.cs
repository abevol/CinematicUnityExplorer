#if MONO
namespace CinematicUnityExplorer.Plugins.Paralives.Mcp
{
    internal static class ParalivesUiQuery
    {
        internal static GameObject FindUiRoot(string baseName)
        {
            string originalName = baseName.Replace("(Clone)", "");
            string cloneName = originalName + "(Clone)";

            GameObject root = UnityReflectionUtility.FindGameObjectByName(cloneName);
            if (root == null)
                root = UnityReflectionUtility.FindGameObjectByName(originalName);

            return root;
        }

        internal static GameObject FindChildByName(GameObject parent, string name)
        {
            if (parent == null)
                return null;

            return FindChildByName(parent.transform, name, 32);
        }

        internal static void VisitDescendants(Transform parent, Action<Transform> visit, Func<bool> shouldStop, int maxDepth)
        {
            if (parent == null || maxDepth < 0 || (shouldStop != null && shouldStop()))
                return;

            foreach (Transform child in parent)
            {
                if (shouldStop != null && shouldStop())
                    return;

                visit(child);
                VisitDescendants(child, visit, shouldStop, maxDepth - 1);
            }
        }

        private static GameObject FindChildByName(Transform parent, string name, int remainingDepth)
        {
            if (parent == null || remainingDepth < 0)
                return null;

            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child.gameObject;

                GameObject found = FindChildByName(child, name, remainingDepth - 1);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
#endif

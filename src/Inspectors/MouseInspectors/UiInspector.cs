using System.Collections;
using UnityEngine.EventSystems;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class UiInspector : MouseInspectorBase
    {
        public static readonly List<GameObject> LastHitObjects = new();

        private static readonly List<GameObject> currentHitObjects = new();

        private const string DEFAULT_INSPECTOR_TITLE = "<b>UI Inspector</b> (press <b>ESC</b> to cancel)";

        public override void OnBeginMouseInspect()
        {
            MouseInspector.Instance.inspectorLabelTitle.text = "<b>UI Inspector</b> (press <b>ESC</b> to cancel)";
            MouseInspector.Instance.objPathLabel.text = "";
        }

        public override void ClearHitData()
        {
            currentHitObjects.Clear();
        }

        public override void OnSelectMouseInspect(Action<GameObject> inspectorAction)
        {
            // need to properly handle inspectorAction here
            LastHitObjects.Clear();
            LastHitObjects.AddRange(currentHitObjects);
            RuntimeHelper.StartCoroutine(SetPanelActiveCoro());
        }

        IEnumerator SetPanelActiveCoro()
        {
            yield return null;
            MouseInspectorResultsPanel panel = UIManager.GetPanel<MouseInspectorResultsPanel>(UIManager.Panels.UIInspectorResults);
            panel.SetActive(true);
            panel.ShowResults();
        }

        public override void UpdateMouseInspect(Vector2 mousePos)
        {
            currentHitObjects.Clear();

            foreach (Canvas canvas in RuntimeHelper.FindObjectsOfTypeAll<Canvas>())
            {
                if (!canvas || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                    continue;

                foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
                {
                    if (!graphic || !graphic.enabled || !graphic.gameObject.activeInHierarchy)
                        continue;

                    if (RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, mousePos, canvas.worldCamera))
                    {
                        if (!currentHitObjects.Contains(graphic.gameObject))
                            currentHitObjects.Add(graphic.gameObject);
                    }
                }
            }

            if (currentHitObjects.Any())
                MouseInspector.Instance.UpdateObjectNameLabel($"Click to view UI Objects under mouse: {currentHitObjects.Count}");
            else
                MouseInspector.Instance.UpdateObjectNameLabel( $"No UI objects under mouse.");
        }

        public override void OnEndInspect()
        {
        }
    }
}

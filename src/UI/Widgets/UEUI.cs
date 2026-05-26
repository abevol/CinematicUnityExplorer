using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.UI.Widgets
{
    internal static class UEUI
    {
        public static readonly Color PanelBg = new(0.1f, 0.1f, 0.1f, 1f);
        public static readonly Color SectionBg = new(0.14f, 0.14f, 0.14f, 1f);
        public static readonly Color MutedText = new(0.65f, 0.65f, 0.65f, 1f);
        public static readonly Color Good = new(0.2f, 0.36f, 0.22f, 1f);
        public static readonly Color Warning = new(0.42f, 0.3f, 0.12f, 1f);
        public static readonly Color Danger = new(0.42f, 0.18f, 0.16f, 1f);

        public static GameObject CreateToolbar(GameObject parent, string name)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(parent, name, false, false, true, true, 5, new Vector4(4, 4, 4, 4), new Color(0.13f, 0.13f, 0.13f, 1f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(row, minHeight: 31, flexibleHeight: 0, flexibleWidth: 9999);
            return row;
        }

        public static Text CreateStatus(GameObject parent, string name, string text = "")
        {
            Text label = UIFactory.CreateLabel(parent, name, text, TextAnchor.MiddleLeft, color: MutedText);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(label.gameObject, minHeight: 22, flexibleHeight: 0, flexibleWidth: 9999);
            return label;
        }

        public static GameObject CreateSection(GameObject parent, string name, string title = null)
        {
            GameObject section = UIFactory.CreateVerticalGroup(parent, name, true, false, true, true, 4, new Vector4(5, 5, 5, 5), SectionBg, TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(section, minHeight: 28, flexibleHeight: 0, flexibleWidth: 9999);

            if (!string.IsNullOrEmpty(title))
            {
                Text label = UIFactory.CreateLabel(section, "Title", $"<b>{title}</b>", TextAnchor.MiddleLeft);
                UIFactory.SetLayoutElement(label.gameObject, minHeight: 22, flexibleHeight: 0, flexibleWidth: 9999);
            }

            return section;
        }

        public static Text AddInfoRow(GameObject parent, string label, string value)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(parent, "InfoRow_" + label, false, false, true, true, 5, default, default, TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(row, minHeight: 22, flexibleHeight: 0, flexibleWidth: 9999);

            Text name = UIFactory.CreateLabel(row, "Label", $"<color=#9fb8d8>{label}</color>", TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(name.gameObject, minWidth: 135, minHeight: 22, flexibleWidth: 0, flexibleHeight: 0);

            Text val = UIFactory.CreateLabel(row, "Value", value ?? "", TextAnchor.UpperLeft);
            val.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetLayoutElement(val.gameObject, minHeight: 22, flexibleHeight: 100, flexibleWidth: 9999);
            return val;
        }

        public static ButtonRef CreateActionButton(GameObject parent, string name, string text, Color? color = null, int width = 100)
        {
            ButtonRef button = UIFactory.CreateButton(parent, name, text, color ?? new Color(0.22f, 0.25f, 0.28f, 1f));
            UIFactory.SetLayoutElement(button.GameObject, minWidth: width, minHeight: 25, flexibleWidth: 0, flexibleHeight: 0);
            return button;
        }

        public static void ClearChildren(GameObject parent)
        {
            if (!parent)
                return;

            for (int i = parent.transform.childCount - 1; i >= 0; i--)
                GameObject.Destroy(parent.transform.GetChild(i).gameObject);
        }
    }
}

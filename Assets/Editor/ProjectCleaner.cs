using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace TinyToysFactory.Editor
{
    public class ProjectCleaner : EditorWindow
    {
        [MenuItem("TinyToys/Deep Project Fixer (Final)")]
        public static void ShowWindow() => GetWindow<ProjectCleaner>("Deep Fixer");

        private void OnGUI()
        {
            if (GUILayout.Button("1. Clean ALL Emojis from Scene & Prefabs")) CleanEmojis();
            if (GUILayout.Button("2. Re-Initialize ALL Templates (for Shop)")) ReinitTemplates();
            if (GUILayout.Button("3. Fix Ghost nulls in PressureDirector")) FixPressurePool();
        }

        private void CleanEmojis()
        {
            var texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                if (t.text.Contains("\ud83d") || t.text.Contains("\ud83e") || t.text.Contains("📦") || t.text.Contains("⏳"))
                {
                    Undo.RecordObject(t, "Clean Emoji");
                    t.text = t.text.Replace("📦", "Order: ").Replace("⏳", "Items: ").Replace("⏱", "Time: ").Replace("✅", "[v]").Replace("❌", "[x]");
                    EditorUtility.SetDirty(t);
                }
            }
            Debug.Log("[DeepFixer] Cleaned emojis from " + texts.Length + " text objects.");
        }

        private void ReinitTemplates()
        {
            var root = GameObject.Find("[FactoryMap]");
            if (!root) { Debug.LogError("Run Full Scene Builder first!"); return; }

            var products = AssetDatabase.FindAssets("t:ProductData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ProductData>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();

            var uis = FindObjectsByType<UpgradeShopUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ui in uis) {
                Undo.RecordObject(ui, "Fix Shop UI");
                // Reset arrays to 3 for each
                if (ui.assemblyButtons.Length < 3) ui.assemblyButtons = new UnityEngine.UI.Button[3];
                if (ui.assemblyLabels.Length < 3) ui.assemblyLabels = new TextMeshProUGUI[3];
                if (ui.paintButtons.Length < 3) ui.paintButtons = new UnityEngine.UI.Button[3];
                if (ui.paintLabels.Length < 3) ui.paintLabels = new TextMeshProUGUI[3];
                EditorUtility.SetDirty(ui);
            }
            Debug.Log("[DeepFixer] Re-initialized Shop UI arrays.");
        }

        private void FixPressurePool()
        {
            var pd = FindFirstObjectByType<PressureDirector>();
            if (pd && pd.eventPool != null)
            {
                Undo.RecordObject(pd, "Fix Pool");
                pd.eventPool.RemoveAll(e => e == null);
                EditorUtility.SetDirty(pd);
                Debug.Log("[DeepFixer] Removed nulls from event pool.");
            }
        }
    }
}

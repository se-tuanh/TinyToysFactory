using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace TinyToysFactory.Editor
{
    public class ChangeUIColorToBlack : EditorWindow
    {
        [MenuItem("TinyToys/Change UI To Black")]
        public static void ApplyBlackColor()
        {
            int changedCount = 0;
            // Màu đen nhạt bán trong suốt
            Color blackColor = new Color(0.1f, 0.1f, 0.1f, 0.85f); 

            bool IsTargetPanel(Image img)
            {
                string n = img.name.ToLower();
                // Check theo tên phổ biến của các Panel/Background
                if (n.Contains("panel") || n.Contains("bg") || n.Contains("board") || n.Contains("slot"))
                    return true;
                    
                // Hoặc nếu nó đang là màu cam mà nãy mình đã đổi
                if (Mathf.Abs(img.color.r - 1f) < 0.05f && Mathf.Abs(img.color.g - 0.6f) < 0.05f)
                    return true;
                    
                return false;
            }

            // 1. Process open Scene
            Image[] sceneImages = Resources.FindObjectsOfTypeAll<Image>();
            foreach (Image img in sceneImages)
            {
                if (img.gameObject.scene.isLoaded && IsTargetPanel(img))
                {
                    img.sprite = null;
                    img.color = blackColor;
                    EditorUtility.SetDirty(img);
                    changedCount++;
                }
            }

            // 2. Process Prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Image[] images = prefab.GetComponentsInChildren<Image>(true);
                    bool modified = false;
                    foreach (Image img in images)
                    {
                        if (IsTargetPanel(img))
                        {
                            img.sprite = null;
                            img.color = blackColor;
                            modified = true;
                            changedCount++;
                        }
                    }
                    if (modified)
                    {
                        EditorUtility.SetDirty(prefab);
                        PrefabUtility.SavePrefabAsset(prefab);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Successfully changed {changedCount} UI panels to Black!");
        }
    }
}

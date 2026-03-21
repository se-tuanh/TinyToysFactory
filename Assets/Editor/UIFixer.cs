using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TinyToysFactory.Editor
{
    public class UIFixer : EditorWindow
    {
        [MenuItem("TinyToysFactory/UI Optimizer")]
        public static void ShowWindow()
        {
            GetWindow<UIFixer>("UI Optimizer - Khắc phục UI Mờ");
        }

        private void OnGUI()
        {
            GUILayout.Label("Tối ưu hóa hiển thị UI (Tránh bị mờ)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Các công cụ này sẽ tự động sửa lỗi UI bị mờ bằng cách căn chỉnh lại Canvas và cấu hình nén ảnh.", MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("1. Tối ưu hóa toàn bộ Canvas trong Scene hiện tại"))
            {
                OptimizeCanvases();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("2. Tối ưu hóa chất lượng hình ảnh (Sprites) trong thư mục Assets"))
            {
                OptimizeSpriteImportSettings();
            }
        }

        private void OptimizeCanvases()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;

            foreach (Canvas canvas in canvases)
            {
                Undo.RecordObject(canvas, "Optimize Canvas");
                canvas.pixelPerfect = true; // Giúp UI pixel sắc nét hơn
                
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    Undo.RecordObject(scaler, "Optimize Canvas Scaler");
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f; // Match giữa width và height
                }
                
                count++;
                EditorUtility.SetDirty(canvas);
            }

            Debug.Log($"[UI Optimizer] Đã tối ưu hóa {count} Canvas trong Scene hiện tại.");
        }

        private void OptimizeSpriteImportSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null && importer.textureType == TextureImporterType.Sprite)
                {
                    bool changed = false;

                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        changed = true;
                    }

                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear; // Hoặc Point nếu là Pixel Art
                        changed = true;
                    }

                    // Vô hiệu hóa mipmap cho UI sprites để tránh bị mờ khi scale
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }

                    if (changed)
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        count++;
                    }
                }
            }

            Debug.Log($"[UI Optimizer] Đã tối ưu hóa {count} file ảnh (Sprites) để hiển thị sắc nét hơn.");
        }
    }
}

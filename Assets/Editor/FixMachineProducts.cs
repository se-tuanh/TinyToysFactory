using UnityEngine;
using UnityEditor;
using System.Linq;

namespace TinyToysFactory.Editor
{
    public class FixMachineProducts : EditorWindow
    {
        [MenuItem("TinyToys/Fix Missing Machine Products")]
        public static void Execute()
        {
            // Find all machines in scene
#if UNITY_2022_2_OR_NEWER
            Machine[] machines = GameObject.FindObjectsByType<Machine>(FindObjectsSortMode.None);
#else
            Machine[] machines = GameObject.FindObjectsOfType<Machine>();
#endif
            
            // Find the Toy Car product data as a default
            ProductData toyCar = AssetDatabase.FindAssets("Product_ToyCar t:ProductData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ProductData>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();

            if (toyCar == null)
            {
                Debug.LogError("[FixMachineProducts] Could not find Product_ToyCar.asset. Please make sure your products exist in Assets/ScriptableObjects/Products/");
                return;
            }

            int fixedCount = 0;
            foreach (var m in machines)
            {
                if (m.assignedProduct == null)
                {
                    m.assignedProduct = toyCar;
                    EditorUtility.SetDirty(m);
                    fixedCount++;
                }
            }

            Debug.Log($"[FixMachineProducts] Successfully assigned default Product (Toy Car) to {fixedCount} machines.");
            if (fixedCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }
    }
}

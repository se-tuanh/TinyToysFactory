using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TinyToysFactory.Editor
{
    public class FinalProjectFixer : EditorWindow
    {
        [MenuItem("TinyToys/Final Project Fixer (Orders & Shop)")]
        public static void Execute()
        {
            // 1. Fix OrderManager
#if UNITY_2022_2_OR_NEWER
            OrderManager om = GameObject.FindAnyObjectByType<OrderManager>();
#else
            OrderManager om = GameObject.FindObjectOfType<OrderManager>();
#endif
            if (om != null)
            {
                om.availableOrders = new List<OrderData>();
                string[] guids = AssetDatabase.FindAssets("t:OrderData", new[] { "Assets/ScriptableObjects/Orders" });
                foreach (string guid in guids)
                {
                    OrderData data = AssetDatabase.LoadAssetAtPath<OrderData>(AssetDatabase.GUIDToAssetPath(guid));
                    if (data != null) om.availableOrders.Add(data);
                }
                om.spawnInterval = 8f; // Ensure reasonable interval
                EditorUtility.SetDirty(om);
                Debug.Log($"[FinalFixer] OrderManager initialized with {om.availableOrders.Count} orders.");
            }

            // 2. Fix UpgradeShopUI
#if UNITY_2022_2_OR_NEWER
            UpgradeShopUI shop = GameObject.FindAnyObjectByType<UpgradeShopUI>();
#else
            UpgradeShopUI shop = GameObject.FindObjectOfType<UpgradeShopUI>();
#endif
            if (shop != null)
            {
                shop.powerRestoreCost = 40;
                shop.workerUpgradeCost = 80;
                shop.bufferUpgradeCost = 60;
                EditorUtility.SetDirty(shop);
                Debug.Log("[FinalFixer] Shop costs reset to defaults (Power=40).");
            }

            // 3. Mark Scene Dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[FinalFixer] Project is ready. Please Run the game and check.");
        }
    }
}

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class FixAndExpandShop : EditorWindow
{
    [MenuItem("Tools/Fix and Expand Shop UI")]
    public static void Execute()
    {
        UpgradeShopUI shop = GameObject.FindObjectOfType<UpgradeShopUI>();
        if (shop == null)
        {
            Debug.LogError("Could not find UpgradeShopUI in scene.");
            return;
        }

        // We need to find the old buttons by name since the fields in UpgradeShopUI changed
        GameObject shopPanel = shop.shopPanel;
        if (shopPanel == null) {
            Debug.LogError("ShopPanel reference is null.");
            return;
        }

        // 1. Setup Assembly Buttons
        Transform assemblyBtnRoot = shopPanel.transform.Find("UpgradeGroup/BuyAssembly"); // Guessed path
        if (assemblyBtnRoot == null) assemblyBtnRoot = FindDeepChild(shopPanel.transform, "BuyAssembly");
        
        if (assemblyBtnRoot != null)
        {
            SetupButtonArray(shop, assemblyBtnRoot, true);
        }

        // 2. Setup Paint Buttons
        Transform paintBtnRoot = shopPanel.transform.Find("UpgradeGroup/BuyPaint");
        if (paintBtnRoot == null) paintBtnRoot = FindDeepChild(shopPanel.transform, "BuyPaint");
        
        if (paintBtnRoot != null)
        {
            SetupButtonArray(shop, paintBtnRoot, false);
        }

        EditorUtility.SetDirty(shop);
        Debug.Log("Shop UI Expanded successfully. Please check the 'assemblyButtons' and 'paintButtons' arrays in the Inspector.");
    }

    private static void SetupButtonArray(UpgradeShopUI shop, Transform template, bool isAssembly)
    {
        List<Button> buttons = new List<Button>();
        List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();

        buttons.Add(template.GetComponent<Button>());
        labels.Add(template.GetComponentInChildren<TextMeshProUGUI>());

        // Clone twice to have 3 total
        for (int i = 1; i < 3; i++)
        {
            GameObject clone = Instantiate(template.gameObject, template.parent);
            clone.name = template.name + "_" + i;
            buttons.Add(clone.GetComponent<Button>());
            labels.Add(clone.GetComponentInChildren<TextMeshProUGUI>());
        }

        if (isAssembly)
        {
            shop.assemblyButtons = buttons.ToArray();
            shop.assemblyLabels = labels.ToArray();
        }
        else
        {
            shop.paintButtons = buttons.ToArray();
            shop.paintLabels = labels.ToArray();
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}

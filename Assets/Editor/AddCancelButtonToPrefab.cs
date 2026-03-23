using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class AddCancelButtonToPrefab : EditorWindow
{
    [MenuItem("Tools/Add Cancel Button to ActiveSlot")]
    public static void Execute()
    {
        string path = "Assets/Prefabs/UI/ActiveSlot.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab == null)
        {
            Debug.LogError("Could not find ActiveSlot prefab at " + path);
            return;
        }

        // Create an instance to modify
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        
        // Find DeliverButton to copy its layout or position
        Transform deliverBtn = instance.transform.Find("DeliverButton");
        if (deliverBtn == null)
        {
            Debug.LogError("Could not find DeliverButton in prefab.");
            DestroyImmediate(instance);
            return;
        }

        // Check if CancelButton already exists
        if (instance.transform.Find("CancelButton") != null)
        {
            Debug.Log("CancelButton already exists.");
            DestroyImmediate(instance);
            return;
        }

        // Create CancelButton (Red)
        GameObject cancelBtnObj = Instantiate(deliverBtn.gameObject, instance.transform);
        cancelBtnObj.name = "CancelButton";
        
        // Adjust Position (Put it between Queue and Deliver or below)
        RectTransform rect = cancelBtnObj.GetComponent<RectTransform>();
        
        // Current Deliver: AnchorMin(0.5, 0.42), AnchorMax(0.96, 0.8)
        // Let's shrink Deliver and put Cancel next to it
        RectTransform deliverRect = deliverBtn.GetComponent<RectTransform>();
        deliverRect.anchorMin = new Vector2(0.5f, 0.42f);
        deliverRect.anchorMax = new Vector2(0.72f, 0.8f); // Shrunk
        
        rect.anchorMin = new Vector2(0.74f, 0.42f); // Cancel button next to it
        rect.anchorMax = new Vector2(0.96f, 0.8f);
        
        // Change color to Red
        Image img = cancelBtnObj.GetComponent<Image>();
        if (img != null) img.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        // Change text to ❌ Hủy
        TextMeshProUGUI txt = cancelBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = "❌ Hủy";

        // Save back to prefab
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);
        
        Debug.Log("Successfully added CancelButton to ActiveSlot prefab and adjusted layout.");
    }
}

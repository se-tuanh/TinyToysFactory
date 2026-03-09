using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A temporary script intended to test the ResourceManager product inventory
/// functionality in a separate Test Scene, as per task requirements.
/// </summary>
public class TestInventoryUI : MonoBehaviour
{
    public ProductData testProduct; // Assign a ProductData
    public TextMeshProUGUI inventoryText; // Assign a UI Text

    private void Start()
    {
        if (ResourceManager.Instance != null && testProduct != null)
        {
            ResourceManager.Instance.OnInventoryChanged.AddListener(UpdateUI);
            UpdateUI(testProduct, 0); // initial load
        }
    }

    public void OnAddButtonClicked()
    {
        if (ResourceManager.Instance != null && testProduct != null)
        {
            ResourceManager.Instance.AddProduct(testProduct, 1);
        }
    }

    public void OnRemoveButtonClicked()
    {
         if (ResourceManager.Instance != null && testProduct != null)
        {
            ResourceManager.Instance.RemoveProduct(testProduct, 1);
        }
    }

    private void UpdateUI(ProductData changedProduct, int newAmount)
    {
        if (changedProduct == testProduct && inventoryText != null)
        {
            inventoryText.text = $"{testProduct.productName} Inventory: {newAmount}";
        }
    }
}

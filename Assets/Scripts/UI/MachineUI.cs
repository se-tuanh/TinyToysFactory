using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MachineUI : MonoBehaviour
{
    public Machine machine;
    
    [Header("UI Elements")]
    public Button workerButton;
    public Image workerButtonImage;
    public TextMeshProUGUI workerButtonText;
    public TextMeshProUGUI machineNameText;
    
    public Image progressBarFill;
    public GameObject progressBarContainer;

    private static readonly Color ColorOn = new Color(0.95f, 0.75f, 0.20f); // WorkerGold
    private static readonly Color ColorOff = new Color(0.3f, 0.3f, 0.3f);

    private void Start()
    {
        if (machine == null) machine = GetComponentInParent<Machine>();
        if (machine == null) return;

        // Self-healing: Create label if null (useful for machines bought in shop)
        if (machineNameText == null)
        {
            SetupAutoLabel();
        }

        if (workerButton != null)
        {
            workerButton.onClick.AddListener(OnWorkerButtonClicked);
        }
    }

    private void SetupAutoLabel()
    {
        // Check if there's already a label child we missed
        var existing = transform.Find("MachineLabelCanvas/NameLabel")?.GetComponent<TextMeshProUGUI>();
        if (existing != null)
        {
            machineNameText = existing;
            return;
        }

        // Create a world-space label above the machine
        GameObject canvasObj = new GameObject("MachineLabelCanvas");
        canvasObj.transform.SetParent(machine.transform);
        canvasObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        
        Canvas c = canvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        
        RectTransform rt = canvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        GameObject textObj = new GameObject("NameLabel");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        machineNameText = textObj.AddComponent<TextMeshProUGUI>();
        machineNameText.fontSize = 24;
        machineNameText.alignment = TextAlignmentOptions.Center;
        machineNameText.color = Color.white;
    }

    private void Update()
    {
        if (machine == null) return;

        // Update Worker Button Visuals
        if (workerButtonImage != null && workerButtonText != null)
        {
            workerButtonImage.color = machine.HasWorker ? ColorOn : ColorOff;
            workerButtonText.text = machine.HasWorker ? "Worker: ON" : "Worker: OFF";
        }

        // Update Machine Name
        if (machineNameText != null && machine.assignedProduct != null)
        {
            string typePrefix = machine.machineType == Machine.MachineType.AssemblyA ? "Ráp" : "Sơn";
            machineNameText.text = $"{typePrefix}: {machine.assignedProduct.productName}";
        }
        else if (machineNameText != null)
        {
            machineNameText.text = machine.name;
        }

        // Update Progress Bar Visuals
        if (progressBarContainer != null && progressBarFill != null)
        {
            bool isRunning = machine.CurrentState == Machine.MachineState.Working;
            progressBarContainer.SetActive(isRunning);
            if (isRunning)
            {
                progressBarFill.fillAmount = machine.Progress;
            }
        }
    }

    private void OnWorkerButtonClicked()
    {
        if (machine != null) machine.ToggleWorker();
    }
}

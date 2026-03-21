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
    
    public Image progressBarFill;
    public GameObject progressBarContainer;

    private static readonly Color ColorOn = new Color(0.95f, 0.75f, 0.20f); // WorkerGold
    private static readonly Color ColorOff = new Color(0.3f, 0.3f, 0.3f);

    private void Start()
    {
        if (machine == null) machine = GetComponentInParent<Machine>();
        if (machine == null) return;

        if (workerButton != null)
        {
            workerButton.onClick.AddListener(OnWorkerButtonClicked);
        }
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

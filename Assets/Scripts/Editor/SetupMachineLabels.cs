using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class SetupMachineLabels : EditorWindow
{
    [MenuItem("Tools/Setup Machine Labels in Scene")]
    public static void Execute()
    {
        Machine[] machines = GameObject.FindObjectsOfType<Machine>();
        int count = 0;

        foreach (var m in machines)
        {
            // 1. Find or Create a World-Space Canvas for the label if not already there
            Transform labelTransform = m.transform.Find("MachineLabelCanvas");
            if (labelTransform == null)
            {
                GameObject canvasObj = new GameObject("MachineLabelCanvas");
                canvasObj.transform.SetParent(m.transform);
                canvasObj.transform.localPosition = new Vector3(0, 2.5f, 0); // Floating above
                
                Canvas c = canvasObj.AddComponent<Canvas>();
                c.renderMode = RenderMode.WorldSpace;
                
                RectTransform rt = canvasObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 50);
                rt.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Tiny scale for world space

                canvasObj.AddComponent<CanvasScaler>();
                
                GameObject textObj = new GameObject("NameLabel");
                textObj.transform.SetParent(canvasObj.transform, false);
                
                TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.text = m.name;
                tmp.fontSize = 24;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                
                // Assign to MachineUI if exists
                MachineUI mui = m.GetComponentInChildren<MachineUI>();
                if (mui != null)
                {
                    mui.machineNameText = tmp;
                    EditorUtility.SetDirty(mui);
                }

                count++;
            }
        }

        Debug.Log($"Successfully setup labels for {count} machines.");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}

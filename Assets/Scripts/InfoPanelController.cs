using TMPro;
using UnityEngine;

public class InfoPanelController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private bool hidePanelOnStart = true;

    [Header("Info Text Fields")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI controlsAndActionsText;
    [SerializeField] private TextMeshProUGUI economyAndRoundsText;
    [SerializeField] private TextMeshProUGUI heroBehaviorText;

    private void Start()
    {
        ApplyDefaultInfoText();

        if (hidePanelOnStart && infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }

    public void OpenInfoPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
        }
    }

    public void CloseInfoPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void ApplyDefaultInfoText()
    {
        if (objectiveText != null)
        {
            objectiveText.text = "Objective: Keep the hero alive through all rounds. Support them with smart pickup drops and economy decisions.";
        }

        if (controlsAndActionsText != null)
        {
            controlsAndActionsText.text = "Controls & Actions: Use Left/Right arrows or UI arrow buttons to rotate camera. Buy Health, Ammo, Damage, and Armor pickups to support the hero.";
        }

        if (economyAndRoundsText != null)
        {
            economyAndRoundsText.text = "Economy & Rounds: Enemy kills grant money, purchases spend money, and intermission gives discounts. Start rounds manually or wait for countdown auto-start.";
        }

        if (heroBehaviorText != null)
        {
            heroBehaviorText.text = "Hero AI: The hero auto-fights, avoids danger, and prioritizes critical pickups when low on key stats.";
        }
    }
}

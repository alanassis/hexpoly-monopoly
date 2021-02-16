using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectController : MonoBehaviour
{
    [HideInInspector]
    public List<ToggleSell> toggles = new List<ToggleSell>();
    [HideInInspector]
    public PlayerUI playerUI;

    [SerializeField]
    private Button confirmButton;

    private void Update()
    {
        int toggledToggles = 0;

        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i].isOn)
            {
                toggledToggles++;
            }
        }

        confirmButton.interactable = toggledToggles == 1;
    }

    public void OnConfirmButtonClick()
    {
        int selectedIndex = 0;

        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i].isOn)
            {
                selectedIndex = toggles[i].tile.index;
            }

            Destroy(toggles[i].gameObject);
        }

        toggles.Clear();

        playerUI.OnSelectControllerConfirm(selectedIndex);
    }
}

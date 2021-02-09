using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SellController : MonoBehaviour
{
    [HideInInspector]
    public List<ToggleSell> toggles = new List<ToggleSell>();
    [HideInInspector]
    public PlayerUI playerUI;
    [HideInInspector]
    public int totalSellCoin = 0;
    [HideInInspector]
    public int currentPlayerCoin = 0;
    [HideInInspector]
    public int neededPrice = 0;

    [SerializeField]
    private Button confirmButton;

    private void Update()
    {
        totalSellCoin = 0;
        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i].isOn)
            {
                totalSellCoin += toggles[i].tile.sellPrice;
            }
        }

        int playerCoinAfterSell = currentPlayerCoin + totalSellCoin;

        confirmButton.interactable = playerCoinAfterSell >= neededPrice;
    }

    public void OnConfirmButtonClick()
    {
        List<int> sellIndexs = new List<int>();

        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i].isOn)
            {
                sellIndexs.Add(toggles[i].tile.index);
            }

            Destroy(toggles[i].gameObject);
        }

        playerUI.OnSellControllerConfirm(sellIndexs.ToArray());

        toggles.Clear();
    }

    public void OnDesistButtonClick()
    {
        playerUI.OnDesistConfirm();
    }
}

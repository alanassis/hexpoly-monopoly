using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public PlayerController controller;

    [Header("Prefabs")]
    [SerializeField] private GameObject toggleCanvas;
    [Header("Placar")]
    [SerializeField] private GameObject inGamePlacar;
    [SerializeField] private TMP_Text inGameNickTexts;
    [SerializeField] private TMP_Text inGameCoinTexts;
    [Header("Alerts")]
    [SerializeField] private GameObject alertPanel;
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private GameObject buyPanel;
    [SerializeField] private GameObject sellPanel;

    private int[] buyOptionsPrices;

    // METHODS TO CALL

    public void SetPlacarPhoto(int index)
    {
        Sprite photo = Resources.Load<Sprite>("Skins/Photo" + (index + 1));
        inGamePlacar.GetComponentsInChildren<Image>()[1].sprite = photo;
    }

    public void SetPlacarGrayedOut()
    {
        inGamePlacar.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }

    public void SetNickAndCoin(string nick, int coin)
    {
        inGameNickTexts.text = nick;
        inGameCoinTexts.text = coin.ToString();
    }

    public void ShowAlertPanel(string message)
    {
        alertPanel.GetComponentInChildren<TMP_Text>().text = message;
        alertPanel.SetActive(true);
    }

    public void HideAlertPanel()
    {
        alertPanel.SetActive(false);
    }

    public void ShowWinner(string winnerNick)
    {
        winnerPanel.GetComponentsInChildren<TMP_Text>()[1].text = winnerNick;
        winnerPanel.SetActive(true);
    }

    public void ShowRolarDado()
    {
        actionPanel.SetActive(true);
    }

    public void ShowBuyPanel(int[] priceList, int minPriceIndex)
    {
        buyPanel.GetComponent<BuyPanel>().prices = priceList;
        buyOptionsPrices = priceList;

        Button[] tempButtons = buyPanel.GetComponentsInChildren<Button>();
        for (int i = 0; i < (tempButtons.Length - 1); i++)
        {
            bool hasNeededCoin = controller.coin >= priceList[i];
            tempButtons[i].interactable = i >= minPriceIndex && hasNeededCoin;
        }

        buyPanel.SetActive(true);
    }

    public void ShowSellPanel(PublicTile[] ownedTiles, int neededPrice)
    {
        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();

        // Configura o botão de confirmação
        SellController sellController = sellPanel.GetComponentInChildren<SellController>();
        sellController.currentPlayerCoin = controller.coin;
        sellController.neededPrice = neededPrice;
        sellController.playerUI = this;

        // Percorre os tile e verifica se é do player
        foreach (Tile tile in tiles)
        {
            // Se não for um Tile ou não for um Tile do Player passa pro próximo
            if (!ownedTiles.Any(t => t.index == tile.index)) continue;

            // Pega a posição da casa na tela e adiciona uma margem para cima
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(tile.transform.position);
            Vector3 screenPos = Camera.main.ViewportToScreenPoint(new Vector3(viewportPos.x, viewportPos.y + 0.1f, viewportPos.z));

            // Cria rotação com a seta do toggle virado para baixo
            Quaternion pointRot = Quaternion.Euler(0f, 0f, 180f);

            // Instancia o toggle em cima do Tile e passa o index para o componente ToggleSell
            ToggleSell toggle = Instantiate(toggleCanvas, screenPos, pointRot, sellPanel.transform).GetComponent<ToggleSell>();
            toggle.tile = ownedTiles.First(t => t.index == tile.index);

            // Adiciona o toggle na lista para uso na confirmação
            sellController.toggles.Add(toggle);
        }

        sellPanel.SetActive(true);
    }

    public void SetPlacarPosition(int index)
    {
        RectTransform rect = inGamePlacar.GetComponent<RectTransform>();
        switch (index)
        {
            case 1:
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                break;
            case 0:
                rect.anchorMin = Vector2.right;
                rect.anchorMax = Vector2.right;
                rect.pivot = Vector2.right;
                rect.anchoredPosition = Vector2.zero;
                break;
            case 2:
                rect.anchorMin = Vector2.up;
                rect.anchorMax = Vector2.up;
                rect.pivot = Vector2.up;
                rect.anchoredPosition = Vector2.zero;
                break;
            case 3:
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = Vector2.zero;
                break;
        }
    }

    // UI BUTTONS

    public void OnActionClick()
    {
        actionPanel.SetActive(false);
        controller.CmdOnActionClick();
    }

    public void OnExitClick()
    {
        controller.ExitGame();
    }

    public void OnBuyClick(int buildingTypeIndex)
    {
        buyPanel.SetActive(false);
        int price = buyOptionsPrices[buildingTypeIndex - 1];
        controller.CmdOnBuyClick((BuildingType)buildingTypeIndex, price);
    }

    public void OnCloseBuyPanelClick()
    {
        buyPanel.SetActive(false);
        controller.CmdOnCloseBuy();
    }

    public void OnSellControllerConfirm(int[] sellIndexs)
    {
        sellPanel.SetActive(false);
        controller.CmdOnSellConfirm(sellIndexs);
    }

    public void OnDesistConfirm()
    {
        sellPanel.SetActive(false);

        controller.CmdOnDesistConfirm();
    }
}

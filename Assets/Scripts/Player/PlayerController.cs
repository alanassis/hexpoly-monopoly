using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public int index;
    [SyncVar]
    public Color color;
    [SyncVar]
    public string nick;
    [SyncVar]
    public int coin = 100000; //CHANGE ME

    public bool isAlive = true;
    public int blockedMoveCount = 0;
    public int lastPosition = 0;
    public int position = 0;

    [HideInInspector]
    public Tile currentTile = null;
    [HideInInspector]
    public SpecialTile currentSpecialTile = null;

    [SerializeField] private PlayerUI playerUI;

    // MONOBEHAVIOUR

    private void Update() => playerUI.SetNickAndCoin(nick, coin);

    [ServerCallback]
    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Tile"))
        {
            currentTile = col.GetComponent<Tile>();
        }
        else if (col.CompareTag("SpecialTile"))
        {
            currentSpecialTile = col.GetComponent<SpecialTile>();
        }
    }

    [ServerCallback]
    private void OnTriggerExit(Collider col)
    {
        if (currentTile != null)
        {
            if (col.gameObject.GetInstanceID() == currentTile.gameObject.GetInstanceID())
            {
                currentTile = null;
            }
        }
        else if (currentSpecialTile != null)
        {
            if (col.gameObject.GetInstanceID() == currentSpecialTile.gameObject.GetInstanceID())
            {
                currentSpecialTile = null;
            }
        }
    }

    // OVERRIDE

    public override void OnStartClient()
    {
        SetSkinAndColorByIndex();
        playerUI.SetPlacarPosition(index);
    }

    // TARGET RPC

    [ClientRpc]
    public void ShowAlertPanel(string message) => playerUI.ShowAlertPanel(message);
    [ClientRpc]
    public void HideAlertPanel() => playerUI.HideAlertPanel();
    [ClientRpc]
    public void ShowWinnerPanel(string winnerNick) => playerUI.ShowWinner(winnerNick);

    [TargetRpc]
    public void ShowRollDicePanel(NetworkConnection target) => playerUI.ShowRolarDado();
    [TargetRpc]
    public void ShowBuyPanel(NetworkConnection target, int[] priceList, int minPriceIndex) => playerUI.ShowBuyPanel(priceList, minPriceIndex);
    [TargetRpc]
    public void ShowSellPanel(NetworkConnection target, PublicTile[] ownedTiles, int neededPrice) => playerUI.ShowSellPanel(ownedTiles, neededPrice);

    // COMMAND TO SERVER

    [Command]
    public void CmdOnActionClick()
    {
        StartCoroutine(GameController.Instance.CallbackConfirmMove());
    }

    [Command]
    public void CmdOnBuyClick(BuildingType type, int buyedPrice)
    {
        GameController.Instance.CallbackPlayerBuyHouse(type, buyedPrice);
    }

    [Command]
    public void CmdOnCloseBuy()
    {
        GameController.Instance.CallbackCancelAction();
    }

    [Command]
    public void CmdOnSellConfirm(int[] sellIndexs)
    {
        GameController.Instance.CallbackSellConfirm(sellIndexs);
    }

    [Command]
    public void CmdOnDesistConfirm()
    {
        isAlive = false;
        nick = nick + " (Anjo)";
        coin = 0;

        RpcSetPlacarGrayedOut();

        StartCoroutine(Death());

        GameController.Instance.CallbackDesistConfirm();
    }

    [ClientRpc]
    private void RpcSetPlacarGrayedOut()
    {
        playerUI.SetPlacarGrayedOut();
    }

    // MOVEMENT

    public IEnumerator MoveToNextTile(int times)
    {
        for (int i = 1; i <= times; i++)
        {
            lastPosition = position;
            if (position < 31)
            {
                position++;
            }
            else
            {
                position = 0;
            }

            if (lastPosition != 31 && position >= 0 && position <= 8)
            {
                transform.Translate(new Vector3(0f, 0f, 1f));
            }
            else if (position >= 8 && position <= 16)
            {
                transform.Translate(new Vector3(1f, 0f, 0f));
            }
            else if (position >= 16 && position <= 24)
            {
                transform.Translate(new Vector3(0f, 0f, -1f));
            }
            else if (position >= 24 || lastPosition == 31)
            {
                transform.Translate(new Vector3(-1f, 0f, 0f));
            }

            if (lastPosition == 31 && position == 0)
            {
                ChangeCoin(GameController.Instance.spawnBonus);
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    public IEnumerator Death()
    {
        // TODO: melhorar animação de morte
        while (transform.position.y < 10f)
        {
            transform.Translate(Vector3.up * 0.2f);
            yield return null;
        }
    }

    // UTILS

    public void ExitGame()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManagerHP.singleton.StopHost();
        }
        // stop client if client-only
        else if (NetworkClient.isConnected)
        {
            NetworkManagerHP.singleton.StopClient();
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            NetworkManagerHP.singleton.StopServer();
        }
    }

    public bool ChangeCoin(int amount)
    {
        if (amount < 0)
        {
            if (coin < -amount) return false;
        }

        coin += amount;
        return true;
    }

    private void SetSkinAndColorByIndex()
    {
        Texture skin = Resources.Load<Texture>("Skins/Player" + (index + 1));
        GetComponentInChildren<Renderer>().material.mainTexture = skin;

        playerUI.SetPlacarPhoto(index);

        if (!hasAuthority) return;

        switch (index + 1)
        {
            case 1:
                CmdSetColor(new Color(0.5f, 0f, 0f));
                break;
            case 2:
                CmdSetColor(new Color(0f, 0.5f, 0f));
                break;
            case 3:
                CmdSetColor(new Color(0f, 0f, 0.5f));
                break;
            case 4:
                CmdSetColor(new Color(0.75f, 0f, 0.75f));
                break;
        }
    }

    [Command]
    private void CmdSetColor(Color newColor)
    {
        color = newColor;
    }
}

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class GameController : NetworkBehaviour
{
    public static GameController Instance;

    public int spawnBonus = 300000;

    [SerializeField]
    private int minRandomMove = 2;
    [SerializeField]
    private int maxRandomMove = 12;
    [SerializeField]
    private int portalRoundBlockCount = 3;
    [SerializeField]
    private float shortAlertMessageDelay = 2f;
    [SerializeField]
    private float longAlertMessageDelay = 4f;
    [SerializeField]
    private float delayBetweenActions = 0.5f;

    private NetworkManagerHP room;
    private int activePlayerIndex;

    private bool waitingForPlayer = false;

    [Server]
    private void Awake()
    {
        Instance = this;
        room = NetworkManagerHP.singleton as NetworkManagerHP;
    }

    [Server]
    private void Start() => StartCoroutine(StartGame());

    [Server]
    private IEnumerator StartGame()
    {
        yield return new WaitForSeconds(2f);

        activePlayerIndex = Random.Range(0, room.roomSlots.Count);

        PlayerController player = UtilGetCurrentPlayer();

        string message = $"Primeiro jogador\n{player.nick}";
        yield return StartCoroutine(AlertAllPlayers(message));

        yield return new WaitForSeconds(delayBetweenActions);

        ActionRollDice();
    }

    [Server]
    private void PassTheTurn()
    {
        if (activePlayerIndex < room.roomSlots.Count - 1)
        {
            activePlayerIndex++;
        }
        else
        {
            activePlayerIndex = 0;
        }

        StartCoroutine(ChooseNextAction());
    }

    [Server]
    private IEnumerator ChooseNextAction()
    {
        PlayerController player = UtilGetCurrentPlayer();

        // Se há dois ou mais jogadores
        if (room.roomSlots.Count > 1)
        {
            // Se o jogador pode andar
            if (player.blockedMoveCount == 0)
            {
                ActionRollDice();
            }
            // Se não pode andar
            else
            {
                yield return StartCoroutine(AlertCannotMove());

                player.blockedMoveCount--;
                PassTheTurn();
            }
        }
        // Se só houver um jogador
        else
        {
            player.ShowWinnerPanel(player.nick);
        }
    }

    [Server]
    private void OnPlayerStopMovement()
    {
        PlayerController player = UtilGetCurrentPlayer();

        // Se o player caiu em um Tile comum
        if (player.currentTile != null)
        {
            OnPlayerStopOnTile();
            return;
        }

        // Se o player caiu em um Tile especial
        if (player.currentSpecialTile != null)
        {
            OnPlayerStopOnSpecialTile();
            return;
        }

        // Se o player caiu em um Tile vazio
        PassTheTurn();
        return;
    }

    [Server]
    private void OnPlayerStopOnTile()
    {
        PlayerController player = UtilGetCurrentPlayer();
        Tile tile = player.currentTile;

        // Se não houver dono no Tile
        if (tile.currentOwner == null)
        {
            StartCoroutine(ActionBuyOptions());
            return;
        }
        // Se houver dono no Tile
        else
        {
            // Se o player atual for dono do Tile
            if (tile.currentOwner.index == player.index)
            {
                StartCoroutine(ActionBuyOptions());
                return;
            }

            // Se o player não for dono do Tile
            StartCoroutine(ActionPayRent());
            return;
        }
    }

    [Server]
    private void OnPlayerStopOnSpecialTile()
    {
        PlayerController player = UtilGetCurrentPlayer();

        switch (player.currentSpecialTile.type)
        {
            case SpecialType.Portal:
                StartCoroutine(PortalController());
                break;
            case SpecialType.Satelite:
                StartCoroutine(SateliteController());
                break;
        }
    }

    // Controllers

    [Server]
    private IEnumerator PortalController()
    {
        PlayerController player = UtilGetCurrentPlayer();

        player.blockedMoveCount = portalRoundBlockCount;

        string message = $"O jogador {player.nick} entrou no portal e se perdeu\nRodadas restantes: {portalRoundBlockCount}";
        yield return StartCoroutine(AlertAllPlayers(message, true));

        PassTheTurn();
    }

    [Server]
    private IEnumerator SateliteController()
    {
        PlayerController player = UtilGetCurrentPlayer();

        string message = $"O jogador {player.nick} ativou os satélites e inaugurou a StarLink";
        yield return StartCoroutine(AlertAllPlayers(message, true));

        PublicTile[] playerTiles = UtilGetCurrentPlayerTiles();

        player.ShowSelectPanel(player.connectionToClient, playerTiles);
    }

    // Callbacks

    [Server]
    public IEnumerator CallbackConfirmMove()
    {
        yield return new WaitForSeconds(delayBetweenActions);

        int randomMoveCount = Random.Range(minRandomMove, maxRandomMove);

        PlayerController player = UtilGetCurrentPlayer();

        player.ShowAlertPanel(player.nick + ": " + randomMoveCount.ToString());

        yield return StartCoroutine(player.MoveToNextTile(randomMoveCount));

        player.HideAlertPanel();

        yield return new WaitForSeconds(delayBetweenActions);

        OnPlayerStopMovement();
    }

    [Server]
    public void CallbackCancelAction()
    {
        waitingForPlayer = false;
        PassTheTurn();
    }

    [Server]
    public void CallbackPlayerBuyHouse(BuildingType type, int buyedPrice)
    {
        PlayerController player = UtilGetCurrentPlayer();
        Tile tile = player.currentTile;

        player.ChangeCoin(-buyedPrice);

        if (tile.currentOwner && tile.currentOwner.index != player.index)
        {
            tile.currentOwner.ChangeCoin(buyedPrice);
        }

        tile.SetBuilding(player, type);

        PassTheTurn();
    }

    [Server]
    public void CallbackSellConfirm(int[] sellIndexs)
    {
        PlayerController player = UtilGetCurrentPlayer();

        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();
        Tile[] selledTiles = tiles.Where(t => sellIndexs.Contains(t.index)).ToArray();

        foreach (Tile tile in selledTiles)
        {
            int price = tile.currentSellPrice;
            player.ChangeCoin(price);

            tile.RemoveBuilding();
        }

        int rentValue = player.currentTile.currentRentPrice;
        player.ChangeCoin(-rentValue);
        player.currentTile.currentOwner.ChangeCoin(rentValue);

        PassTheTurn();
    }

    [Server]
    public void CallbackDesistConfirm()
    {
        PlayerController player = UtilGetCurrentPlayer();

        // Remove o jogador da lista de jogadores ativos
        room.roomSlots.RemoveAt(activePlayerIndex);

        // Busca todos tiles do mapa
        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();

        // Percorre os tiles e apagam os que forem do player
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i].currentOwner == null) continue;
            if (tiles[i].currentOwner.index == player.index)
            {
                tiles[i].RemoveBuilding();
            }
        }

        waitingForPlayer = false;
    }

    [Server]
    public void CallbackSelectConfirm(int selectedIndex)
    {
        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();
        Tile selectedTile = tiles.Single(t => t.index == selectedIndex);

        selectedTile.isStarlinkOn = true;

        PassTheTurn();
    }

    // Actions

    [Server]
    private void ActionRollDice()
    {
        PlayerController player = UtilGetCurrentPlayer();
        player.ShowRollDicePanel(player.connectionToClient);
    }

    [Server]
    private IEnumerator ActionBuyOptions()
    {
        PlayerController player = UtilGetCurrentPlayer();
        Tile tile = player.currentTile;

        bool isUpgrade = tile.currentOwner && tile.currentOwner.index == player.index;
        bool canUpgrade = tile.currentBuildingType != (BuildingType)System.Enum.GetNames(typeof(BuildingType)).Length - 1;

        if (isUpgrade && !canUpgrade)
        {
            PassTheTurn();
            yield break;
        }

        StartCoroutine(PlayerCheckAndShowBuyPanel(tile));
    }

    [Server]
    private IEnumerator ActionPayRent()
    {
        PlayerController player = UtilGetCurrentPlayer();
        Tile tile = player.currentTile;

        waitingForPlayer = true;
        bool payed = PlayerPayToAnotherPlayer(tile.currentOwner, player.currentTile.currentRentPrice);

        // Se pagou com sucesso
        if (payed)
        {
            waitingForPlayer = false;
            StartCoroutine(ActionBuyOptions());
            yield break;
        }

        // Se não pagou, espera pela venda
        while (waitingForPlayer == true)
        {
            yield return null;
        }

        if (!player.isAlive)
        {
            PassTheTurn();
            yield break;
        }

        StartCoroutine(ActionBuyOptions());
    }

    [Server]
    private IEnumerator ActionSell(int neededPrice, NoMoneyAction action)
    {
        PlayerController player = UtilGetCurrentPlayer();

        yield return StartCoroutine(AlertNoMoney(action == NoMoneyAction.Rent ? "para pagar aluguel" : ""));

        PublicTile[] playerTiles = UtilGetCurrentPlayerTiles();

        player.ShowSellPanel(player.connectionToClient, playerTiles, neededPrice);
    }

    // Alerts

    [Server]
    private IEnumerator AlertCannotMove()
    {
        PlayerController player = UtilGetCurrentPlayer();

        player.ShowAlertPanel($"{player.nick} voltará em {player.blockedMoveCount} rodadas");

        yield return new WaitForSeconds(shortAlertMessageDelay);

        player.HideAlertPanel();

        yield return new WaitForSeconds(delayBetweenActions);
    }

    [Server]
    private IEnumerator AlertNoMoney(string additionalMsg = "")
    {
        PlayerController player = UtilGetCurrentPlayer();

        player.ShowAlertPanel(player.nick + ": Não tem dinheiro " + additionalMsg);

        yield return new WaitForSeconds(shortAlertMessageDelay);

        player.HideAlertPanel();

        yield return new WaitForSeconds(delayBetweenActions);
    }

    [Server]
    private IEnumerator AlertAllPlayers(string message, bool longDelay = false)
    {
        PlayerController player = UtilGetCurrentPlayer();

        player.ShowAlertPanel(message);

        yield return new WaitForSeconds(longDelay ? longAlertMessageDelay : shortAlertMessageDelay);

        player.HideAlertPanel();

        yield return new WaitForSeconds(delayBetweenActions);
    }

    // UTILS

    [Server]
    private IEnumerator PlayerCheckAndShowBuyPanel(Tile tile)
    {
        PlayerController player = UtilGetCurrentPlayer();

        // Index do menor valor possível
        int minPriceIndex = (int)tile.currentBuildingType;
        // Array de preços
        int[] buyPrice = new int[tile.defaultBuyPrice.Length];

        // Cópia os valores padrões para o novo array
        tile.defaultBuyPrice.CopyTo(buyPrice, 0);

        // Se for Tile de outro jogador
        if (tile.currentOwner && tile.currentOwner.index != player.index)
        {
            minPriceIndex--;
            for (int i = 0; i < buyPrice.Length; i++)
            {
                buyPrice[i] *= 2;
            }
        }

        // Menor valor possível
        int minPrice = buyPrice[minPriceIndex];

        // Se o jogador não tiver dinheiro para nenhuma opção
        if (player.coin < minPrice)
        {
            yield return StartCoroutine(AlertNoMoney("para comprar imóvel"));
            PassTheTurn();

            yield break;
        }

        player.ShowBuyPanel(player.connectionToClient, buyPrice, minPriceIndex);
        yield break;
    }

    [Server]
    private bool PlayerPayToAnotherPlayer(PlayerController receiver, int amount)
    {
        PlayerController payer = UtilGetCurrentPlayer();

        bool success = payer.ChangeCoin(-amount);

        if (!success)
        {
            StartCoroutine(ActionSell(amount, NoMoneyAction.Rent));
            return false;
        }

        receiver.ChangeCoin(amount);
        return true;
    }

    [Server]
    private PlayerController UtilGetCurrentPlayer()
    {
        NetworkIdentity currentIdentity = room.roomSlots[activePlayerIndex].connectionToClient.identity;
        PlayerController player = currentIdentity.GetComponent<PlayerController>();

        return player;
    }

    [Server]
    private PublicTile[] UtilGetCurrentPlayerTiles()
    {
        PlayerController player = UtilGetCurrentPlayer();

        Tile[] allTiles = GameObject.FindObjectsOfType<Tile>();

        List<PublicTile> playerTiles = new List<PublicTile>();

        for (int i = 0; i < allTiles.Length; i++)
        {
            if (allTiles[i] && allTiles[i].currentOwner)
            {
                if (allTiles[i].currentOwner.index == player.index)
                {
                    PublicTile temp = new PublicTile();
                    temp.index = allTiles[i].index;
                    temp.sellPrice = allTiles[i].currentSellPrice;

                    playerTiles.Add(temp);
                }
            }
        }

        return playerTiles.ToArray();
    }
}

enum NoMoneyAction
{
    Buy, Rent
}

public struct PublicTile
{
    public int index;
    public int sellPrice;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class Tile : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField]
    private float yDif;
    [SerializeField]
    private string localName;

    [Header("Price")]
    public int[] defaultBuyPrice = new int[3];

    [Header("Canvas UI")]
    [SerializeField]
    private TMP_Text localNameText;
    [SerializeField]
    private TMP_Text priceText;

    [HideInInspector]
    public PlayerController currentOwner = null;
    [HideInInspector]
    public GameObject currentHouse = null;
    [HideInInspector]
    [SyncVar(hook = nameof(OnBuildingTypeChange))]
    public BuildingType currentBuildingType = BuildingType.None;
    [HideInInspector]
    [SyncVar(hook = nameof(OnStarlinkChange))]
    public bool isStarlinkOn = false;
    [HideInInspector]
    public int currentRentPrice
    {
        get
        {
            if (currentBuildingType == BuildingType.None) return 0;

            float val = (defaultBuyPrice[(int)currentBuildingType - 1] / 100f) * (index * 5);
            int finalVal = isStarlinkOn == false ? Mathf.RoundToInt(val) : Mathf.RoundToInt(val) * 2;
            return finalVal;
        }
    }
    [HideInInspector]
    public int currentSellPrice
    {
        get
        {
            return (defaultBuyPrice[(int)currentBuildingType - 1] / 2);
        }
    }

    public int index;

    private void Start()
    {
        index = int.Parse(name.Split('(', ')')[1]);

        localNameText.text = localName.ToUpper();

        // TODO: pensar em uma lógica melhor :P
        for (int i = 0; i < defaultBuyPrice.Length; i++)
        {
            float juros = (defaultBuyPrice[i] / 100f) * (index * 5);
            defaultBuyPrice[i] += Mathf.RoundToInt(juros);
        }

        SetPriceText();
    }

    [Server]
    public void SetBuilding(PlayerController newOwner, BuildingType type)
    {
        RemoveBuilding();

        GameObject prefab = Resources.Load<GameObject>("Houses/" + type);

        Vector3 xPosDiff = transform.forward * 0.2f;
        currentHouse = Instantiate(prefab, transform.position + xPosDiff, Quaternion.Euler(0f, yDif, 0f));

        NetworkServer.Spawn(currentHouse);

        currentHouse.GetComponent<HouseController>().SetColor(newOwner.color);
        currentBuildingType = type;
        currentOwner = newOwner;
    }

    [Server]
    public void RemoveBuilding()
    {
        if (currentHouse != null)
        {
            NetworkServer.Destroy(currentHouse);
            currentHouse = null;
        }

        currentBuildingType = BuildingType.None;
        currentOwner = null;
    }

    private void OnBuildingTypeChange(BuildingType _, BuildingType newB) => SetPriceText();

    private void OnStarlinkChange(bool _, bool newV) => SetPriceText();

    public void SetPriceText()
    {
        priceText.text = currentRentPrice == 0 ? "" : FormatNumber(currentRentPrice);
    }

    private static string FormatNumber(long num)
    {
        // Ensure number has max 3 significant digits (no rounding up can happen)
        long i = (long)Mathf.Pow(10, (int)Mathf.Max(0, Mathf.Log10(num) - 2));
        num = num / i * i;

        if (num >= 1000000000)
            return (num / 1000000000D).ToString("0.##") + "B";
        if (num >= 1000000)
            return (num / 1000000D).ToString("0.##") + "M";
        if (num >= 1000)
            return (num / 1000D).ToString("0.##") + "K";

        return num.ToString("#,0");
    }
}

public enum BuildingType
{
    None, Casa1, Casa2, Casa3
}
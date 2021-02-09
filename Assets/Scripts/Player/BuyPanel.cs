using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BuyPanel : MonoBehaviour
{
    [HideInInspector]
    public int[] prices = new int[3];

    [SerializeField]
    private TMP_Text[] pricesText = new TMP_Text[3];

    private void Update()
    {
        if (pricesText.Length != prices.Length) return;

        for (int i = 0; i < pricesText.Length; i++)
        {
            pricesText[i].text = "$ " + prices[i].ToString();
        }
    }
}

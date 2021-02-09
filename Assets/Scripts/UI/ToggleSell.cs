using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSell : MonoBehaviour
{
    public PublicTile tile;
    public bool isOn = false;
    
    public void SetOn(bool newValue)
    {
        isOn = newValue;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class SpecialTile : NetworkBehaviour
{
    public SpecialType type;
}

public enum SpecialType
{
    Portal
}
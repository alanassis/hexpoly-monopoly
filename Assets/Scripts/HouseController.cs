using UnityEngine;
using Mirror;

public class HouseController : NetworkBehaviour
{
    [ClientRpc]
    public void SetColor(Color32 color)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        for (int i = 0; i < renderer.materials.Length; i++)
        {
            if (renderer.materials[i].name.Contains("roof"))
            {
                renderer.materials[i].color = color;
            }
        }
    }
}

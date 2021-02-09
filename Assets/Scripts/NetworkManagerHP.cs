using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using NobleConnect.Mirror;

public class NetworkManagerHP : NobleRoomManager
{
    private Vector3[] spawnPoints = new Vector3[4]
    {
        new Vector3(-4.25f, 0.2f, -3.75f),
        new Vector3(-4.25f, 0.2f, -4.25f),
        new Vector3(-3.75f, 0.2f, -4.25f),
        new Vector3(-3.75f, 0.2f, -3.75f)
    };

    private int spawnPointIndex = 0;

    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection conn, GameObject roomPlayer)
    {
        GameObject player = Instantiate(playerPrefab, spawnPoints[spawnPointIndex++], Quaternion.identity);
        return player;
    }

    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnection conn, GameObject roomPlayer, GameObject gamePlayer)
    {
        PlayerController controller = gamePlayer.GetComponent<PlayerController>();
        controller.index = roomPlayer.GetComponent<NetworkRoomPlayerHP>().index;
        controller.nick = roomPlayer.GetComponent<NetworkRoomPlayerHP>().displayName;

        int index = roomPlayer.GetComponent<NetworkRoomPlayerHP>().index;
        controller.color = index == 0 ? Color.red : index == 1 ? Color.green : index == 2 ? Color.blue : Color.yellow;

        return true;
    }

    public override void OnRoomStopClient()
    {
        // Demonstrates how to get the Network Manager out of DontDestroyOnLoad when
        // going to the offline scene to avoid collision with the one that lives there.
        if (gameObject.scene.name == "DontDestroyOnLoad" && !string.IsNullOrEmpty(offlineScene) && SceneManager.GetActiveScene().path != offlineScene)
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

        base.OnRoomStopClient();
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Mirror;
using NobleConnect.Mirror;

public class NetworkRoomPlayerHP : NobleRoomPlayer
{
    [SerializeField] GameObject roomPanel;
    [SerializeField] TMP_Text[] displayNameTexts = new TMP_Text[4];
    [SerializeField] TMP_Text[] isReadyTexts = new TMP_Text[4];
    [SerializeField] TMP_InputField addressText;
    [SerializeField] TMP_Text readyUpButtonText;

    [SyncVar]
    public string displayName = "Loading...";

    private NetworkManagerHP room;
    private Scene activeScene;
    private string encodedAddress = "";

    // MONOBEHAVIOUR

    private void Update()
    {
        if (!hasAuthority) return;

        if (room.HostEndPoint != null)
        {
            if (encodedAddress == "")
            {
                encodedAddress = room.HostEndPoint.Address.ToString() + ":" + room.HostEndPoint.Port.ToString();
                addressText.text = encodedAddress;
            }
        }

        if (activeScene.name == room.RoomScene || activeScene.path == room.RoomScene)
        {
            UpdatePlayersReadyUp();
        }
        else
        {
            roomPanel.SetActive(false);
        }
    }

    // OVERRIDE

    public override void OnStartAuthority()
    {
        room = NetworkManagerHP.singleton as NetworkManagerHP;
        activeScene = SceneManager.GetActiveScene();
        SetDisplayName(MenuUI.PlayerNick);
    }

    [Command]
    private void SetDisplayName(string nick) => displayName = nick;

    public override void ReadyStateChanged(bool _, bool newReadyState) => UpdatePlayersReadyUp();

    // UI

    private void UpdatePlayersReadyUp()
    {
        if (!hasAuthority) return;
        roomPanel.SetActive(true);

        for (int i = 0; i < room.roomSlots.Count; i++)
        {
            displayNameTexts[i].text = room.roomSlots[i].GetComponent<NetworkRoomPlayerHP>().displayName;
            isReadyTexts[i].text = room.roomSlots[i].GetComponent<NetworkRoomPlayerHP>().readyToBegin ? "<color=green>OK</color>" : "NÃO";
        }
    }

    public void OnReadyUpButtonClick()
    {
        readyUpButtonText.text = readyToBegin ? "PRONTO" : "CANCELAR";
        CmdChangeReadyState(!readyToBegin);
    }
}

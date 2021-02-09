using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class MenuUI : MonoBehaviour
{
    [SerializeField]
    private InputField nickInputField;
    [SerializeField]
    private TMP_Text addressTitleText;
    [SerializeField]
    private InputField addressInputField;
    [SerializeField]
    private Button joinButton;

    [SerializeField]
    private GameObject startPanel;
    [SerializeField]
    private NetworkManagerHP managerHP;

    private string playerNickPrefsName = "PlayerNick";
    private bool isClient = false;

    public static string PlayerNick { get; private set; }

    private void Start()
    {
        if (PlayerPrefs.HasKey(playerNickPrefsName))
        {
            string nick = PlayerPrefs.GetString(playerNickPrefsName);

            nickInputField.text = nick;
            SetPlayerNick(nick);
        }
    }

    private void Update()
    {
        if (startPanel.activeSelf)
        {
            if (NetworkClient.active)
            {
                if (NetworkClient.isConnected)
                {
                    startPanel.SetActive(false);
                }
                else
                {
                    joinButton.GetComponentInChildren<TMP_Text>().text = "...";
                    joinButton.interactable = false;
                }
            }
            else
            {
                string joinText = isClient ? "Iniciar" : "Entrar\nna sala";
                joinButton.GetComponentInChildren<TMP_Text>().text = joinText;
                joinButton.interactable = true;
            }
        }
    }

    public void OnHostButtonClick()
    {
        managerHP.StartHost();
    }

    public void OnJoinButtonClick()
    {
        if (isClient)
        {
            managerHP.StartClient();
        }
        else
        {
            managerHP.InitClient();

            addressTitleText.gameObject.SetActive(true);
            addressInputField.gameObject.SetActive(true);

            isClient = true;
        }
    }

    private void SetPlayerNick(string nick)
    {
        if (string.IsNullOrEmpty(nick.Trim())) return;

        PlayerNick = nick;
        PlayerPrefs.SetString(playerNickPrefsName, nick);
    }

    public void OnNickInputChange(string nick)
    {
        SetPlayerNick(nick);
        joinButton.interactable = !string.IsNullOrEmpty(nick.Trim());
    }

    public void OnIpInputChange(string ip)
    {
        if (!string.IsNullOrEmpty(ip))
        {
            string[] address = ip.Split(':');
            managerHP.networkAddress = address[0];

            if (address.Length == 2 && !string.IsNullOrEmpty(address[1]))
            {
                managerHP.networkPort = ushort.Parse(address[1]);
            }
        }
    }
}

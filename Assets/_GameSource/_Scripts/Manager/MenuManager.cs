using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main menu / lobby flow: connects to Photon, handles nickname and XP display,
/// and starts either a random matchmade game or a friend's room by ID.
/// </summary>
public class MenuManager : MonoBehaviourPunCallbacks
{
    #region Fields

    [Header("UI REFERENCES")]
    [Space]
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomIDInput;
    [SerializeField] private GameObject friendPanel;
    public TextMeshProUGUI playerXPText;

    [Header("VARIABLES")]
    [Space]
    public string playerName;
    private readonly string gameVersion = "0.1";
    public string friendRoomID;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Start clean: disconnect any lingering session and lock the button
        // until OnConnected/OnJoinedLobby confirms we're actually online.
        PhotonNetwork.Disconnect();
        playButton.interactable = false;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = false;

        LoadSavedPlayerName();
        LoadSavedPlayerXP();
    }

    #endregion

    #region Photon Callbacks

    public override void OnConnected()
    {
        base.OnConnected();
        Debug.Log("CONNECTED SERVER");

        playButton.interactable = true;
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        PhotonNetwork.JoinLobby();
        PhotonNetwork.NickName = playerName;

        Debug.Log("CONNECTION STATUS " + PhotonNetwork.IsConnected);
        Debug.Log(PhotonNetwork.CloudRegion);
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.Log("JOIN LOBBY");

        playButton.interactable = true;
    }

    public override void OnLeftLobby()
    {
        base.OnLeftLobby();
        Debug.Log("LEFT LOBBY");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);
        Debug.Log(returnCode + message);
    }

    #endregion

    #region Room / Matchmaking

    /// <summary>
    /// Joins a random open room (or creates one) for a standard 1v1 match.
    /// </summary>
    public void JoinGame()
    {
        if (!PhotonNetwork.IsConnected) return;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;

        PhotonNetwork.JoinRandomOrCreateRoom(null, 2, MatchmakingMode.FillRoom, null, null, null, roomOptions, null);
        PhotonNetwork.LoadLevel(1);
    }

    /// <summary>
    /// Joins a specific friend's room using the ID previously set via <see cref="RoomIDSet"/>.
    /// </summary>
    public void JoinGameFriend()
    {
        if (!PhotonNetwork.IsConnected) return;

        PhotonNetwork.JoinRoom(friendRoomID);
        PhotonNetwork.LoadLevel(1);
    }

    #endregion

    #region UI Helpers

    public void RoomIDSet(string roomID)
    {
        friendRoomID = roomID;
    }

    public void OpenURLButton(string link)
    {
        Application.OpenURL(link);
    }

    public void ChangeNickName(string name)
    {
        playerName = name;
        PhotonNetwork.NickName = playerName;

        PlayerPrefs.SetString("playerName", name);
        playerNameInput.text = name;
    }

    #endregion

    #region Saved Data

    private void LoadSavedPlayerName()
    {
        if (PlayerPrefs.HasKey("playerName"))
        {
            string savedName = PlayerPrefs.GetString("playerName");
            ChangeNickName(savedName);
        }
    }

    private void LoadSavedPlayerXP()
    {
        if (PlayerPrefs.HasKey("PlayerXP"))
        {
            playerXPText.text = "XP : " + PlayerPrefs.GetInt("PlayerXP").ToString();
        }
        else
        {
            PlayerPrefs.SetInt("PlayerXP", 0);
        }
    }

    #endregion
}
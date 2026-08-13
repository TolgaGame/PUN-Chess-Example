using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// Handles the core match flow for a 2-player online chess match built on Photon PUN:
/// room join/leave, player spawning, camera setup, start/finish states and the "opponent left" check.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    #region Fields

    [Header("REFERENCES")]
    [Tooltip("Central PhotonView used to broadcast RPCs to all players in the room.")]
    public PhotonView photonViews;

    [Tooltip("Reference to the chess board / gameplay logic component.")]
    public BoardManager boardManager;

    [Header("STATE")]
    public bool isGameStarted;

    [Header("PLAYERS")]
    public Player player1;
    public Player player2;

    // Cached player camera GameObjects, populated via GetPlayerTransforms RPC.
    public GameObject[] playerObjects;

    [Header("TRANSFORM")]
    [Space]
    public Transform spawnPos_1;
    public Transform spawnPos_2; // Spawn position for player 2

    // Camera rotation applied to each player on spawn.
    private readonly Quaternion cameraAngle_Player1 = Quaternion.Euler(45, 0, 0);
    private readonly Quaternion cameraAngle_Player2 = Quaternion.Euler(45, -180, 0);

    [Header("PANELS")]
    [Space]
    public Transform waitingPanel;
    public Transform startPanel;
    public Transform finishPanel;

    [Header("TEXTS")]
    [Space]
    public TextMeshProUGUI roomIDText;
    public TextMeshProUGUI finishText;

    #endregion

    #region Unity Lifecycle

    private void Start() {
        isGameStarted = false;
    }

    private void OnApplicationQuit()
    {
        FinishGame();
    }

    #endregion

    #region Photon Callbacks

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        Debug.Log("JOINED ROOM");

        roomIDText.text = "ROOM ID: " + PhotonNetwork.CurrentRoom.Name;

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            // First player in the room: show the waiting screen and spawn at slot 1.
            waitingPanel.gameObject.SetActive(true);

            PhotonNetwork.Instantiate("ChessPlayer", spawnPos_1.position, cameraAngle_Player1);
            photonViews.RPC("GetPlayerTransforms", RpcTarget.AllBuffered);
        }
        else if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            // Second player joined: spawn at slot 2 and start the match.
            PhotonNetwork.Instantiate("ChessPlayer", spawnPos_2.position, cameraAngle_Player2);
            photonViews.RPC("GetPlayerTransforms", RpcTarget.AllBuffered);

            ReadyGame();
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        Debug.Log("LEFT ROOM");
        FinishGame();
        PhotonNetwork.LoadLevel(0);
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        Debug.Log("CREATED ROOM");
    }

    #endregion

    #region Public API

    public void ExitRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void FinishGame()
    {
        photonViews.RPC("GameFinished", RpcTarget.AllBuffered);

        int previousXP = PlayerPrefs.GetInt("PlayerXP");
        PlayerPrefs.SetInt("PlayerXP", previousXP + 10);
    }

    #endregion

    #region Match Flow

    /// <summary>
    /// Called once the room has 2 players. Notifies everyone the game has started
    /// and kicks off the camera setup for each client.
    /// </summary>
    private void ReadyGame()
    {
        Debug.Log("ROOM FULL - READY GAME");

        photonViews.RPC("GameStarted", RpcTarget.AllBuffered);
        photonViews.RPC("DisplayPlayerList", RpcTarget.AllBuffered);

        StartCoroutine(SetCameraView());
    }

    [PunRPC]
    public void GameStarted()
    {
        waitingPanel.gameObject.SetActive(false);
        startPanel.gameObject.SetActive(true);

        isGameStarted = true;

        // Periodically verify the opponent is still connected.
        InvokeRepeating(nameof(CheckPlayerCount), 1f, 30f);
    }

    [PunRPC]
    private void GameFinished()
    {
        finishPanel.gameObject.SetActive(true);
        boardManager.enabled = false;

        Debug.Log("GAME FINISHED");
    }

    #endregion

    #region Player / Camera Setup

    [PunRPC]
    private void DisplayPlayerList()
    {
        Dictionary<int, Player> players = PhotonNetwork.CurrentRoom.Players;

        foreach (KeyValuePair<int, Player> playerEntry in players)
        {
            int playerNumber = playerEntry.Key;
            Player player = playerEntry.Value;

            if (playerNumber == 1)
            {
                player1 = player;
                Debug.Log("Player 1: " + player.NickName);
            }
            else if (playerNumber == 2)
            {
                player2 = player;
                Debug.Log("Player 2: " + player.NickName);
            }
        }

        photonView.TransferOwnership(player1);
    }

    [PunRPC]
    private void GetPlayerTransforms()
    {
        playerObjects = GameObject.FindGameObjectsWithTag("MainCamera");
    }

    [PunRPC]
    private void SetCamera()
    {
        // Disable every camera that doesn't belong to the local player.
        for (int i = 0; i < playerObjects.Length; i++)
        {
            if (playerObjects[i].GetComponent<PhotonView>().IsMine == false)
            {
                playerObjects[i].GetComponent<Camera>().enabled = false;
            }
        }
    }

    private IEnumerator SetCameraView()
    {
        // Small delay to make sure both players' camera objects exist before filtering.
        yield return new WaitForSeconds(0.1f);

        photonViews.RPC("SetCamera", RpcTarget.AllBuffered);
    }

    #endregion

    #region Disconnect Handling

    private void CheckPlayerCount()
    {
        // If the opponent's camera object is gone, treat it as them having left.
        if (playerObjects[1] == null)
        {
            FinishGame();
        }
    }

    #endregion
}
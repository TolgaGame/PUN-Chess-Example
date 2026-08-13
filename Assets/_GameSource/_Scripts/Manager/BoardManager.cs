using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Core chess gameplay logic: board state, piece selection/movement, spawning,
/// turn switching, en-passant/promotion handling and end-of-game detection.
/// Runs in sync across clients via Photon RPCs.
/// </summary>
public class BoardManager : MonoBehaviourPunCallbacks
{
    #region Fields

    [Header("REFERENCES")]
    public GameManager gameManager;
    public PhotonView photonViews;

    public static BoardManager Instance { get; set; }

    // Legal-move grid for the currently selected chessman.
    private bool[,] allowedMoves { get; set; }

    [Header("BOARD METRICS")]
    [Space]
    private const float TILE_SIZE = 1.0f;
    private const float TILE_OFFSET = 0.5f;

    [Header("SELECTION")]
    [Space]
    private int selectionX = -1;
    private int selectionY = -1;

    // [0]=x, [1]=y of a tile currently capturable via en-passant, or -1,-1 if none.
    public int[] EnPassantMove { set; get; }

    public bool isWhiteTurn = true;
    public bool endGame = false;

    [Header("CHESSMAN OBJECTS")]
    [Space]
    public List<GameObject> chessmanPrefabs;
    private List<GameObject> activeChessman;

    private readonly Quaternion whiteOrientation = Quaternion.Euler(0, 270, 0);
    private readonly Quaternion blackOrientation = Quaternion.Euler(0, 90, 0);

    public Chessman[,] Chessmans { get; set; }
    private Chessman selectedChessman;

    [Header("SELECTION MATERIAL")]
    private Material previousMat;
    public Material selectedMat;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Instance = this;
        SpawnAllChessmans();
        EnPassantMove = new int[2] { -1, -1 };
    }

    private void Update()
    {
        if (!gameManager.isGameStarted) return;

        UpdateSelection();

        if (!photonView.IsMine) return;

        if (Input.GetMouseButtonDown(0) && selectionX >= 0 && selectionY >= 0)
        {
            if (selectedChessman == null)
            {
                photonViews.RPC("SelectChessman", RpcTarget.AllBuffered, selectionX, selectionY);
            }
            else
            {
                photonViews.RPC("MoveChessman", RpcTarget.AllBuffered, selectionX, selectionY);
            }
        }
    }

    #endregion

    #region Selection & Movement (RPCs)

    [PunRPC]
    private void SelectChessman(int x, int y)
    {
        if (Chessmans[x, y] == null) return;
        if (Chessmans[x, y].isWhite != isWhiteTurn) return;

        allowedMoves = Chessmans[x, y].PossibleMoves();

        if (!HasAtLeastOneMove(allowedMoves)) return;

        selectedChessman = Chessmans[x, y];

        // Swap in the "selected" material while keeping the piece's original texture.
        previousMat = selectedChessman.GetComponent<MeshRenderer>().material;
        selectedMat.mainTexture = previousMat.mainTexture;
        selectedChessman.GetComponent<MeshRenderer>().material = selectedMat;

        BoardHighlights.Instance.HighLightAllowedMoves(allowedMoves);
    }

    [PunRPC]
    private void MoveChessman(int x, int y)
    {
        if (allowedMoves[x, y])
        {
            Chessman target = Chessmans[x, y];

            // Capture an enemy piece on the destination tile, if any.
            if (target != null && target.isWhite != isWhiteTurn)
            {
                if (target.GetType() == typeof(King))
                {
                    photonViews.RPC("EndGame", RpcTarget.AllBuffered);
                    endGame = true;
                    return;
                }

                activeChessman.Remove(target.gameObject);
                Destroy(target.gameObject);
            }

            // Handle en-passant capture.
            if (x == EnPassantMove[0] && y == EnPassantMove[1])
            {
                Chessman capturedPawn = isWhiteTurn ? Chessmans[x, y - 1] : Chessmans[x, y + 1];

                activeChessman.Remove(capturedPawn.gameObject);
                Destroy(capturedPawn.gameObject);
            }

            EnPassantMove[0] = -1;
            EnPassantMove[1] = -1;

            // Handle pawn promotion and set up a fresh en-passant opportunity.
            if (selectedChessman.GetType() == typeof(Pawn))
            {
                if (y == 7) // White promotion
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    SpawnChessman(1, x, y, true);
                    selectedChessman = Chessmans[x, y];
                }
                else if (y == 0) // Black promotion
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    SpawnChessman(7, x, y, false);
                    selectedChessman = Chessmans[x, y];
                }

                EnPassantMove[0] = x;
                if (selectedChessman.CurrentY == 1 && y == 3)
                    EnPassantMove[1] = y - 1;
                else if (selectedChessman.CurrentY == 6 && y == 4)
                    EnPassantMove[1] = y + 1;
            }

            // Move the piece into its new board slot.
            Chessmans[selectedChessman.CurrentX, selectedChessman.CurrentY] = null;
            selectedChessman.transform.position = GetTileCenter(x, y);
            selectedChessman.SetPosition(x, y);
            Chessmans[x, y] = selectedChessman;

            if (!endGame)
            {
                isWhiteTurn = !isWhiteTurn;
            }

            // Hand turn ownership over to whichever player's color is now active.
            if (isWhiteTurn)
            {
                photonViews.TransferOwnership(gameManager.player1);
            }
            else
            {
                photonViews.TransferOwnership(gameManager.player2);
            }
        }

        // Reset selection visuals regardless of whether the move was legal.
        selectedChessman.GetComponent<MeshRenderer>().material = previousMat;
        BoardHighlights.Instance.HideHighlights();
        selectedChessman = null;
    }

    /// <summary>
    /// Returns true if the given legal-move grid contains at least one selectable move.
    /// </summary>
    private bool HasAtLeastOneMove(bool[,] moves)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (moves[i, j]) return true;
            }
        }

        return false;
    }

    #endregion

    #region Input / Raycast

    private void UpdateSelection()
    {
        if (!Camera.main) return;

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 50.0f, LayerMask.GetMask("ChessPlane")))
        {
            selectionX = (int)hit.point.x;
            selectionY = (int)hit.point.z;
        }
        else
        {
            selectionX = -1;
            selectionY = -1;
        }
    }

    #endregion

    #region Spawning

    private void SpawnChessman(int index, int x, int y, bool isWhite)
    {
        Vector3 position = GetTileCenter(x, y);
        Quaternion orientation = isWhite ? whiteOrientation : blackOrientation;

        GameObject chessmanObject = Instantiate(chessmanPrefabs[index], position, orientation);
        chessmanObject.transform.SetParent(transform);

        Chessmans[x, y] = chessmanObject.GetComponent<Chessman>();
        Chessmans[x, y].SetPosition(x, y);

        activeChessman.Add(chessmanObject);
    }

    private void SpawnAllChessmans()
    {
        activeChessman = new List<GameObject>();
        Chessmans = new Chessman[8, 8];

        /////// White ///////

        SpawnChessman(0, 3, 0, true); // King
        SpawnChessman(1, 4, 0, true); // Queen

        SpawnChessman(2, 0, 0, true); // Rooks
        SpawnChessman(2, 7, 0, true);

        SpawnChessman(3, 2, 0, true); // Bishops
        SpawnChessman(3, 5, 0, true);

        SpawnChessman(4, 1, 0, true); // Knights
        SpawnChessman(4, 6, 0, true);

        for (int i = 0; i < 8; i++) // Pawns
        {
            SpawnChessman(5, i, 1, true);
        }

        /////// Black ///////

        SpawnChessman(6, 4, 7, false); // King
        SpawnChessman(7, 3, 7, false); // Queen

        SpawnChessman(8, 0, 7, false); // Rooks
        SpawnChessman(8, 7, 7, false);

        SpawnChessman(9, 2, 7, false); // Bishops
        SpawnChessman(9, 5, 7, false);

        SpawnChessman(10, 1, 7, false); // Knights
        SpawnChessman(10, 6, 7, false);

        for (int i = 0; i < 8; i++) // Pawns
        {
            SpawnChessman(11, i, 6, false);
        }
    }

    #endregion

    #region Board Utility

    private Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += (TILE_SIZE * x) + TILE_OFFSET;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET;

        return origin;
    }

    #endregion

    #region Game End

    [PunRPC]
    private void EndGame()
    {
        Debug.Log("END GAME");

        gameManager.finishText.text = isWhiteTurn ? "WHITE WINS " : "BLACK WINS ";

        foreach (GameObject chessmanObject in activeChessman)
        {
            Destroy(chessmanObject);
        }

        BoardHighlights.Instance.HideHighlights();

        gameManager.FinishGame();
    }

    #endregion
}
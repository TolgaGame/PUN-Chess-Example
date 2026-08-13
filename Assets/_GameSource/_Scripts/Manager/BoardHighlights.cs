using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a reusable pool of "highlight" objects used to mark legal move tiles
/// on the chess board (pooled instead of instantiated/destroyed every move).
/// </summary>
public class BoardHighlights : MonoBehaviour
{
    #region Fields

    public static BoardHighlights Instance { get; set; }

    [Tooltip("Prefab spawned/pooled to visually mark a highlighted tile.")]
    public GameObject highlightPrefab;

    // Pool of highlight instances, reused instead of destroyed to avoid GC churn.
    private List<GameObject> highlights;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Instance = this;
        highlights = new List<GameObject>();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Activates and positions a highlight tile for every legal move flagged in the given grid.
    /// </summary>
    /// <param name="moves">8x8 grid where true marks a tile as a legal move.</param>
    public void HighLightAllowedMoves(bool[,] moves)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (moves[i, j])
                {
                    GameObject highlight = GetHighlightObject();
                    highlight.SetActive(true);
                    highlight.transform.position = new Vector3(i + 0.5f, 0.0001f, j + 0.5f);
                }
            }
        }
    }

    /// <summary>
    /// Deactivates every pooled highlight (does not destroy them, so they can be reused).
    /// </summary>
    public void HideHighlights()
    {
        foreach (GameObject highlight in highlights)
        {
            highlight.SetActive(false);
        }
    }

    #endregion

    #region Pooling

    /// <summary>
    /// Returns an inactive highlight from the pool, or instantiates a new one if none is free.
    /// </summary>
    private GameObject GetHighlightObject()
    {
        GameObject highlight = highlights.Find(g => !g.activeSelf);

        if (highlight == null)
        {
            highlight = Instantiate(highlightPrefab);
            highlights.Add(highlight);
        }

        return highlight;
    }

    #endregion
}
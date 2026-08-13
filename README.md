# ♟️ Online Chess (Unity + Photon PUN)

A real-time, 1v1 multiplayer chess game built with **Unity** and **Photon PUN 2**, featuring room-based matchmaking, full chess rule logic (including en-passant and pawn promotion), synced camera/turn ownership, and a simple XP/rewards loop.

> ⚠️ **Archive / Portfolio Notice**
> This is an **older personal project** that has been pulled out of storage, **cleaned up, commented, and reorganized** to be published here as a **code sample / portfolio piece**. It is **not actively maintained** and isn't meant to be a production-ready game — it's shared to demonstrate multiplayer architecture, gameplay-system design, and general C# code quality. Expect some legacy quirks and a few TODOs along the way.

---

## 🎮 What It Does

- Two players connect through Photon, get matched into a room, and play a full game of chess in real time.
- All board logic (legal moves, captures, en-passant, pawn promotion, check-for-king-capture end state) runs synchronized across both clients via Photon RPCs.
- Each player only controls their own camera/view; turn ownership is transferred between players as the game progresses.
- A lightweight menu flow handles connecting to Photon, setting a nickname, quick-matchmaking or joining a friend's room by ID.
- A basic XP counter and rewarded/interstitial ad hooks are included as a stand-in for a simple progression/monetization loop.

## 🧩 Core Systems

| Script | Responsibility |
|---|---|
| `MenuManager` | Connects to Photon, handles nickname/XP setup, matchmaking (random or friend room ID) |
| `GameManager` | Room lifecycle (join/leave), player spawning, camera setup, game start/finish state |
| `BoardManager` | Chess rules: selection, movement, captures, en-passant, promotion, turn switching, end-game detection |
| `BoardHighlights` | Pooled highlight tiles for showing legal moves on the board |
| `Chessman` (+ piece subclasses) | Per-piece move generation logic |
| `AdManager` | Banner / interstitial / rewarded ad hooks |

## 🛠️ Tech Stack

- **Engine:** Unity
- **Networking:** [Photon PUN 2](https://www.photonengine.com/pun) (rooms, RPCs, ownership transfer)
- **UI:** TextMeshPro
- **Persistence:** `PlayerPrefs` for nickname/XP (simple local storage, no backend)

## 📁 Project Structure (high level)

```
Assets/
 ├─ Scripts/
 │   ├─ MenuManager.cs
 │   ├─ GameManager.cs
 │   ├─ BoardManager.cs
 │   ├─ BoardHighlights.cs
 │   ├─ Chessman.cs (+ King, Queen, Rook, Bishop, Knight, Pawn)
 │   └─ AdManager.cs
 ├─ Prefabs/
 ├─ Scenes/
 └─ ...
```

## ▶️ Running the Project

1. Clone the repo.
2. Open the project in Unity (see `ProjectSettings` for the exact version used).
3. Import/configure **Photon PUN 2** with your own `PhotonServerSettings` App ID.
4. Open the Menu scene and press Play — two instances (or two builds) are needed to test a full match.

## 🧹 Cleanup Notes

As part of preparing this repo for publishing, the scripts have been:

- Reorganized into clearly labeled `#region` blocks (fields, lifecycle, RPCs, gameplay, utility, etc.)
- Documented with XML/inline comments explaining intent, especially around Photon RPC flow and chess-rule edge cases (en-passant, promotion, turn ownership transfer)
- Lightly refactored for readability (extracted small helper methods, clearer local variable names)

**Public/serialized fields, RPC method names, and Inspector-facing method signatures were deliberately left unchanged** to avoid breaking existing scene/prefab references — so a few original naming quirks (and at least one known typo in a cross-script method call) are intentionally still present rather than silently "fixed."

## 🗺️ Known Limitations / Possible Next Steps

- No check/checkmate detection — the game currently ends only on a king capture.
- No reconnection/resume handling if a player disconnects mid-match.
- No backend/leaderboard — XP is stored locally per device.
- Minimal input validation and error handling around Photon connection failures.

## 📄 License

This project is shared for **educational and portfolio purposes**. Feel free to read through the code and borrow ideas — see the repository's license file (or open an issue) for reuse terms.

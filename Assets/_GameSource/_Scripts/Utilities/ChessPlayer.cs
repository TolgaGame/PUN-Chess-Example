using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class ChessPlayer : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI nicknameText;

    private void Start()
    {
        nicknameText.text = PhotonNetwork.NickName;
    }


}
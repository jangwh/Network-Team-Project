using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class FinishLine : MonoBehaviourPun
{
    private void OnTriggerEnter(Collider other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        // --- 마스터에게 신고
        photonView.RPC("RPC_ReportFinish", RpcTarget.MasterClient,
            pv.Owner.ActorNumber, (double)StageManager.Instance.ElapsedTime);

        // 로컬 플레이어 카메라 비활성 & 관전 모드 전환
        var pc = other.GetComponent<PlayerController>();
        if (pc != null && pc.Camera != null)
            pc.Camera.gameObject.SetActive(false);
        if (pc != null) pc.checkpointIndex = -1;
        pc.checkpointIndex = -1;

        SpectatorManager.Instance.EnterSpectatorMode();
        SpectatorManager.Instance.RemoveTarget(other.transform);

        // 파괴는 1초 지연
        StartCoroutine(DelaySecond());
        PhotonNetwork.Destroy(other.gameObject);
    }

    IEnumerator DelaySecond()
    {
        yield return new WaitForSeconds(1f);
    }

    // --- MasterClient에서 실행 ---
    [PunRPC]
    void RPC_ReportFinish(int actorNumber, double finishTime)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        RaceManager.Instance.RegisterFinish(actorNumber, (float)finishTime);
    }
}
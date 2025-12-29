using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisappearPlatform : MonoBehaviourPun
{
    public GameObject platform;
    public MeshRenderer meshRenderer;
    private bool isTriggered = false;

    void OnCollisionEnter(Collision collision)
    {
        if (isTriggered) return; // 중복 방지
        if (collision.gameObject.CompareTag("Player"))
        {
            isTriggered = true;
            photonView.RPC("StartDisappear", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void StartDisappear()
    {
        StartCoroutine(Dissappear());
    }

    IEnumerator Dissappear()
    {
        meshRenderer.materials[0].color = Color.black;

        yield return new WaitForSeconds(1f);

        platform.SetActive(false); // 네트워크 전체에서 꺼짐
    }
}

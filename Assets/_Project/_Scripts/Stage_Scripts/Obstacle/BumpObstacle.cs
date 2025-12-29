using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BumpObstacle : MonoBehaviour
{
    public float bumpSpeed; //밀려날 힘 Inspector에서 조절 가능
    //죄송합니다테스트좀하겠습니다.
    //

    void OnCollisionEnter(Collision collision)
    {
        //밀려날 쪽의 Rigidbody
        Rigidbody rigid = collision.rigidbody; 

        //플레이어 위치 - 장애물 위치 = 튕겨나갈 방향
        Vector3 dir = (collision.transform.position - transform.position).normalized;

        //y는 무시하고 수평 방향만 사용
        dir.y = 0f;

        rigid.AddForce(dir * bumpSpeed, ForceMode.Impulse);

        //죄송합니다테스트좀하겠습니다.
        PhotonView view = collision.gameObject.GetComponent<PhotonView>();
        if (view != null && view.IsMine)
        {
            SFXEvents.Raise(SFXKey.Bump, collision.transform.position, true, true);
            //MapObjSFXPlayManager.Instance.PlaySFX(SFXKey.Bump, true, collision.transform.position, true);
        }
        //
    }
}

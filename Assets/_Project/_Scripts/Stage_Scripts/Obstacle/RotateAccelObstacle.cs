using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAccelObstacle : MonoBehaviour
{
    public float rotateSpeed; //회전할 힘 Inspector에서 조절 가능 
    void OnCollisionStay(Collision collision) 
    {
        //플레이어 Transfrom 정보 가져옴
        Transform trans = collision.transform;

        //회전에 맞춰 플레이어의 위치 변경
        trans.RotateAround(transform.position, Vector3.up, rotateSpeed); 
        trans.rotation = Quaternion.identity;
    }
}

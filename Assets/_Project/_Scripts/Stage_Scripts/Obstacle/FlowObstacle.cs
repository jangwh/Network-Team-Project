using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowObstacle : MonoBehaviour
{
    public float flowSpeed; //미끄러질 힘 Inspector에서 조절 가능

    void OnCollisionStay(Collision collision)
    {
        //아래쪽으로 미끄러짐
        collision.gameObject.GetComponent<Rigidbody>().AddForce(Vector3.back * flowSpeed, ForceMode.Impulse);
    }
}

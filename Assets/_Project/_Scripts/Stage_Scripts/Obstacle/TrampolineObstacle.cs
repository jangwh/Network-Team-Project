using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrampolineObstacle : MonoBehaviour
{
    public float jumpPower; //튕겨나갈 힘 Inspector에서 조절 가능
    public float x; //튕겨나갈 방향 Inspector에서 조절 가능
    public float y;
    public float z;

    void OnCollisionEnter(Collision collision)
    {
        //설정해둔 방향으로 튕겨나감
        collision.gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(x, y, z) * jumpPower, ForceMode.Impulse);
        //LSH테스트
        SFXEvents.Raise(SFXKey.Trampoline, transform.position, true, true);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public int checkpointNum;
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.UpdateCheckpoint(checkpointNum); // 이 오브젝트의 번호
            }
        }
    }
}

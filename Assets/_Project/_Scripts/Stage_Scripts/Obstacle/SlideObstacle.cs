using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideObstacle : MonoBehaviour
{
    [SerializeField] private Transform targetPos;
    [SerializeField] private float slideSpeed = 8f;
    private void Awake()
    {
        // MeshCollider라도 반드시 isTrigger = true 로 설정
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var move = other.GetComponent<TempMoveScript>();
        if (move != null) move.DisableInput();  // 플레이어 조작 잠금
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player") || targetPos == null) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        // 목표 지점 방향으로 지속적으로 속도 갱신
        Vector3 dir = (targetPos.position - other.transform.position).normalized;
        rb.velocity = dir * slideSpeed;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Rigidbody rb = other.attachedRigidbody;

        var move = other.GetComponent<TempMoveScript>();
        if (move != null) move.EnableInput();   // 조작 복원
    }
}

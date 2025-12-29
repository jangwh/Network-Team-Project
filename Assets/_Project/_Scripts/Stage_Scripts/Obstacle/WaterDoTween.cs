using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class WaterDoTween : MonoBehaviour
{
    public float waterUpValue;
    public float waterUpDuration;

    Transform waterTrans;

    void Awake()
    {
        waterTrans = GetComponent<Transform>();
    }

    void Start()
    {
        waterUp();
    }

    void waterUp()
    {
        waterTrans.DOLocalMoveY(waterUpValue, waterUpDuration).SetDelay(10.0f);
    }
}

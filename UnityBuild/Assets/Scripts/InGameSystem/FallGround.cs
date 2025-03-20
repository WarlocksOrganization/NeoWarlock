using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

public class FallGround : MonoBehaviour
{
    [SerializeField] private GameObject projector;
    
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void NextFall()
    {
        if (projector != null)
        {
            projector.SetActive(true);
        }
    }

    public void Fall()
    {
        if (projector != null)
        {
            projector.SetActive(false);
        }
        
        rb.isKinematic = false; // ✅ 중력 적용 가능하게 변경
        rb.useGravity = true;
        
        Vector3 randomForce = new Vector3(
            Random.Range(-1f, 1f), // X 방향 랜덤 힘
            Random.Range(1f, 2f),  // Y 방향 (살짝 위로 밀어줌)
            Random.Range(-1f, 1f)  // Z 방향 랜덤 힘
        );
                
        rb.AddForce(randomForce, ForceMode.Impulse);
    }
}

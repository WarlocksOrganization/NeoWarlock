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
        
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // ✅ 모든 자식 오브젝트의 Collider 비활성화
        Collider[] childColliders = GetComponentsInChildren<Collider>();
        foreach (var col in childColliders)
        {
            col.enabled = false;
        }

        rb.isKinematic = false; // 중력 적용 가능하게 변경
        rb.useGravity = true;

        Vector3 randomForce = new Vector3(
            transform.position.normalized.x*Random.Range(1, 3f),
            Random.Range(5f, 10f),
            transform.position.normalized.z*Random.Range(1, 3f)
        );

        rb.AddForce(randomForce, ForceMode.Impulse);
        
        Vector3 randomTorque = new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f)
        );

        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }

}

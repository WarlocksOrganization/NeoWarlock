using System.Collections;
using Mirror;
using UnityEngine;

public class InvokeDestroy : NetworkBehaviour
{
    [SerializeField] private float destroyTime = 5f;
    public override void OnStartServer()
    {
        StartCoroutine(AutoDestroy());
    }
    
    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(destroyTime);

        if (isServer)
        {
            NetworkServer.Destroy(gameObject); // ✅ 서버에서만 제거
        }
        else
        {
            Destroy(gameObject); // ✅ 클라이언트에서도 안전하게 제거
        }
    }
}

using UnityEngine;

public class GameSystemManager : MonoBehaviour
{
    public static GameSystemManager Instance;

    [SerializeField] private GameObject[] FallGrounds;
    private int eventnum = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // ✅ 중복된 Instance 제거
    }

    public void StartEvent()
    {
        if (FallGrounds == null || FallGrounds.Length == 0)
        {
            //Debug.LogError("❌ FallGrounds가 설정되지 않았습니다!");
            return;
        }

        // ✅ 현재 이벤트의 FallGround 자식 찾기 및 실행
        if (eventnum < FallGrounds.Length)
        {
            GameObject selectedGround = FallGrounds[eventnum];

            if (selectedGround != null)
            {
                // ✅ 현재 FallGround의 모든 자식 오브젝트에 Fall() 실행
                FallGround[] fallGrounds = selectedGround.GetComponentsInChildren<FallGround>();

                if (fallGrounds.Length > 0)
                {
                    foreach (var fallGround in fallGrounds)
                    {
                        fallGround.Fall();
                    }
                }
                else
                {
                    //Debug.LogWarning($"⚠️ 이벤트 {eventnum}: 자식 FallGround가 없습니다.");
                }
            }
            else
            {
                //Debug.LogError($"❌ 이벤트 {eventnum}: 선택된 FallGround가 존재하지 않습니다!");
            }
        }
    }
    public void NetEvent()
    {
    // ✅ 다음 이벤트의 FallGround 자식 찾기 및 NextFall() 실행
        int nextEvent = eventnum + 1;
        if (nextEvent < FallGrounds.Length)
        {
            GameObject nextGround = FallGrounds[nextEvent];

            if (nextGround != null)
            {
                // ✅ 다음 FallGround의 모든 자식 오브젝트에 NextFall() 실행
                FallGround[] nextFallGrounds = nextGround.GetComponentsInChildren<FallGround>();

                if (nextFallGrounds.Length > 0)
                {
                    foreach (var nextFallGround in nextFallGrounds)
                    {
                        nextFallGround.NextFall();
                    }
                }
            }
        }
        eventnum++; // 다음 이벤트로 이동
    }
}

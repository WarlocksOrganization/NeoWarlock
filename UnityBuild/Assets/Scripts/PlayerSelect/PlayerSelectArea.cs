using System.Collections.Generic;
using UnityEngine;
using DataSystem;

[System.Serializable]
public class CharacterModelEntry
{
    public Constants.CharacterClass characterClass;
    public GameObject characterModel;
}

public class PlayerSelectArea : MonoBehaviour
{
    [Header("캐릭터 모델 리스트")]
    public List<CharacterModelEntry> characterModelList = new List<CharacterModelEntry>();

    private Dictionary<Constants.CharacterClass, GameObject> characterModelDict;
    private GameObject activeCharacter;
    private Coroutine rotationCoroutine;

    [Header("Layer 설정")]
    [SerializeField] private string defaultLayerName = "Special";  // 기본 레이어 이름
    [SerializeField] private string outlineLayerName = "Select";  // Outline용 레이어 이름

    private int defaultLayer;
    private int outlineLayer;

    [Header("UI Manager")]
    public CharacterSelectionManager characterSelectionManager; // ✅ 스킬 UI를 업데이트할 UI 매니저 추가

    void Awake()
    {
        InitializeCharacterDictionary();

        // ✅ LayerMask를 int (Layer Index)로 변환
        defaultLayer = LayerMask.NameToLayer(defaultLayerName);
        outlineLayer = LayerMask.NameToLayer(outlineLayerName);
    }

    private void InitializeCharacterDictionary()
    {
        characterModelDict = new Dictionary<Constants.CharacterClass, GameObject>();

        foreach (var entry in characterModelList)
        {
            if (entry.characterModel != null && !characterModelDict.ContainsKey(entry.characterClass))
            {
                characterModelDict.Add(entry.characterClass, entry.characterModel);
                entry.characterModel.GetComponent<Animator>().SetFloat("Blend", (int)entry.characterClass);
            }
            else
            {
                Debug.LogWarning($"{entry.characterClass}에 대한 중복 데이터가 있거나 GameObject가 없습니다!");
            }
        }

        Debug.Log("캐릭터 모델 Dictionary 초기화 완료.");
    }

    public void SelectCharacter(Constants.CharacterClass selectedClass)
    {
        // ✅ 기존 활성화된 캐릭터 비활성화 및 레이어 복구
        if (activeCharacter != null)
        {
            activeCharacter.GetComponent<Animator>().SetBool("isSelect", false);
            SetLayerRecursively(activeCharacter.transform, defaultLayer); // 기존 캐릭터의 모든 자식 Layer 복구
        }

        // ✅ 선택된 캐릭터 찾기
        if (characterModelDict.TryGetValue(selectedClass, out GameObject character))
        {
            activeCharacter = character;
            activeCharacter.GetComponent<Animator>().SetBool("isSelect", true);
            SetLayerRecursively(activeCharacter.transform, outlineLayer); // ✅ 선택된 캐릭터와 모든 자식 Layer 변경

            RotateCharacterToTarget((int)selectedClass * 72, selectedClass); // ✅ 목표 각도로 회전
        }
        else
        {
            Debug.LogWarning($"{selectedClass}에 대한 모델이 없습니다!");
        }
    }

    private void RotateCharacterToTarget(float targetAngle, Constants.CharacterClass selectedClass)
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }

        // ✅ PlayerSelectArea 오브젝트 자체를 회전
        rotationCoroutine = StartCoroutine(SmoothRotate(this.transform, targetAngle, selectedClass));
    }

    private System.Collections.IEnumerator SmoothRotate(Transform targetTransform, float targetAngle, Constants.CharacterClass selectedClass)
    {
        float currentAngle = targetTransform.eulerAngles.y;
        float shortestRotation = Mathf.DeltaAngle(currentAngle, targetAngle);
        float finalTargetAngle = currentAngle + shortestRotation;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newAngle = Mathf.Lerp(currentAngle, finalTargetAngle, elapsed / duration);
            targetTransform.rotation = Quaternion.Euler(0, newAngle, 0);
            yield return null;
        }

        // ✅ 최종 보정
        targetTransform.rotation = Quaternion.Euler(0, finalTargetAngle, 0);
        // 추가 : 캐릭터 별 스킬 정보 전달
        if (characterSelectionManager != null)
        {
            characterSelectionManager.SelectCharacter(selectedClass.ToString());
        }
        else
        {
            Debug.LogError("❌ CharacterSelectionManager가 null입니다! PlayerSelectArea의 Inspector에서 연결했는지 확인하세요.");
        }
    }


    private System.Collections.IEnumerator SmoothRotate(GameObject character, float targetAngle)
    {
        float currentAngle = character.transform.eulerAngles.y;
        float shortestRotation = Mathf.DeltaAngle(currentAngle, targetAngle); // ✅ 가장 짧은 회전 방향 계산
        float finalTargetAngle = currentAngle + shortestRotation;

        float duration = 0.5f; // ✅ 회전 속도 조절
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float newAngle = Mathf.Lerp(currentAngle, finalTargetAngle, elapsed / duration);
            character.transform.rotation = Quaternion.Euler(0, newAngle, 0);
            yield return null;
        }

        // ✅ 최종 회전 값 보정
        character.transform.rotation = Quaternion.Euler(0, finalTargetAngle, 0);
    }
    
    private void SetLayerRecursively(Transform parent, int layer)
    {
        parent.gameObject.layer = layer; // 부모 오브젝트 Layer 변경

        foreach (Transform child in parent) // 모든 자식 순회
        {
            SetLayerRecursively(child, layer); // 재귀적으로 변경
        }
    }
}

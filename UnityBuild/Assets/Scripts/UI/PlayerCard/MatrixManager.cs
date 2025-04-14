using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameManagement;
using GameManagement.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class MatrixManager : MonoBehaviour
{    
    public static MatrixManager Instance { get; private set; }

    private MatrixDocument currentMatrix;
    
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadMatrixFromPersistentOrResources();
    }

    public MatrixDocument LoadMatrixFromJson(string jsonText)
    {
        try
        {
            // 최상위 구조를 먼저 파싱
            var root = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonText);

            // isOk 존재 확인
            if (!root.ContainsKey("isOk") || !(bool)(root["isOk"]))
            {
                MatrixLoadState.HasMatrixData = false;
                Debug.LogWarning("[MatrixManager] 서버 응답에 isOk=false 또는 누락됨. 콜드 스타트 상황.");
                return null;
            }

            // data를 다시 json 문자열로 추출 후 기존 구조에 맞춰 파싱
            var jObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonText);
            var dataToken = jObject["data"];

            if (dataToken == null || dataToken.Type != JTokenType.Array)
            {
                MatrixLoadState.HasMatrixData = false;
                Debug.LogError("[MatrixManager] data 항목이 배열이 아니거나 존재하지 않음.");
                return null;
            }

            var wrapper = new MatrixDocumentWrapper
            {
                data = dataToken.ToObject<List<MatrixDocument>>() ?? new List<MatrixDocument>()
            };

            if (wrapper.data.Count == 0)
            {
                Debug.LogWarning("[MatrixManager] data 배열은 존재하지만 비어있음.");
                // 필요하면 여기서 return null 해도 됨
            }

            int classCode = (int)PlayerSetting.PlayerCharacterClass;

            var matrix = wrapper.data.FirstOrDefault(doc =>
                doc.type == "T" && doc.id.Split('/')[3] == classCode.ToString());

            if (matrix == null)
            {
                MatrixLoadState.HasMatrixData = false;
                Debug.LogError($"[MatrixManager] 일치하는 매트릭스 문서가 없습니다: classCode={classCode}");
                return null;
            }

            MatrixLoadState.HasMatrixData = true;
            // matrixMap이 영행렬인 경우
            bool isAllDiagonalZero = matrix.matrixMap.Values
                .SelectMany(list => list)
                .Where(row => row.Count > 0)
                .Select((row, idx) => row[Mathf.Min(idx, row.Count - 1)])
                .All(x => x == 0);
            if (isAllDiagonalZero)
            {
                Debug.LogWarning("[MatrixManager] 매트릭스의 모든 대각선 값이 0입니다.");
            }
            MatrixLoadState.IsMatrixValid = !isAllDiagonalZero;
            currentMatrix = matrix;
            return matrix;
        }
        catch (Exception e)
        {
            MatrixLoadState.HasMatrixData = false;
            Debug.LogError($"[MatrixManager] 매트릭스 파싱 중 예외 발생: {e.Message}");
            return null;
        }
    }

    public MatrixDocument LoadMatrixFromPersistentOrResources()
    {
        string jsonText = null;

        // 우선 persistentDataPath에서 시도
        if (MatrixFileManager.HasMatrixFile())
        {
            jsonText = MatrixFileManager.LoadMatrixJson();
            Debug.Log("[MatrixManager] PersistentData에서 매트릭스 파일 로드");
        }

        if (string.IsNullOrEmpty(jsonText))
        {
            MatrixLoadState.HasMatrixData = false;
            Debug.LogError("[MatrixManager] 매트릭스 데이터를 불러올 수 없습니다.");
            return null;
        }

        return LoadMatrixFromJson(jsonText);
    }

    public MatrixDocument GetMatrix()
    {
        return currentMatrix;
    }
    
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameManagement;
using GameManagement.Data;
using Newtonsoft.Json;

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
        MatrixDocumentWrapper wrapper = JsonConvert.DeserializeObject<MatrixDocumentWrapper>(jsonText);

        int classCode = (int)PlayerSetting.PlayerCharacterClass;

        var matrix = wrapper.data.FirstOrDefault(doc =>
            doc.type == "T" && doc.id.Split('/')[3] == classCode.ToString());

        if (matrix == null)
        {
            Debug.LogError($"[MatrixManager] 매트릭스 문서가 없습니다: classCode={classCode}");
        }

        currentMatrix = matrix;
        return matrix;
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
        else
        {
            // 없으면 Resources에서 fallback
            var textAsset = Resources.Load<TextAsset>("Data/CardMatrixList");
            if (textAsset != null)
            {
                jsonText = textAsset.text;
                Debug.Log("[MatrixManager] Resources에서 매트릭스 파일 로드");
            }
        }

        if (string.IsNullOrEmpty(jsonText))
        {
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

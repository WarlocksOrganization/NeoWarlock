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

        LoadMatrixFromResources();
    }
    public void LoadMatrixFromResources()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Data/CardMatrixList");
        
        if (textAsset == null)
        {
            Debug.LogError("[MatrixManager] CardMatrixList.txt 파일을 Resources에서 찾을 수 없습니다.");
            return;
        }

        LoadMatrixFromJson(textAsset.text);
    }
    public void LoadMatrixFromJson(string jsonText)
    {
        MatrixDocumentWrapper wrapper = JsonConvert.DeserializeObject<MatrixDocumentWrapper>(jsonText);

        int classCode = (int)PlayerSetting.PlayerCharacterClass;

        currentMatrix = wrapper.data.FirstOrDefault(doc =>
            doc.type == "T" && doc.id.Split('/')[3] == classCode.ToString());

        if (currentMatrix == null)
        {
            Debug.LogError($"[MatrixManager] 매트릭스 문서가 없습니다: classCode={classCode}");
        }
    }
    public MatrixDocument GetMatrix(int classCode)
    {
        return currentMatrix;
    }
}

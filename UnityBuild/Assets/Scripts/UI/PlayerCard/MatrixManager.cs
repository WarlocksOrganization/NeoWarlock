using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameManagement;
using GameManagement.Data;

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
        var wrapper = JsonUtility.FromJson<MatrixDocumentWrapper>(jsonText);

        int classCode = PlayerSetting.SelectedClassCode;
        Debug.Log($"[MatrixManager] 불러온 클래스 코드: {classCode}");

        foreach (var doc in wrapper.Documents)
        {
            Debug.Log($"[MatrixManager] 문서: id={doc.Id}, split[3]={doc.Id.Split('/')[3]}");
        }

        currentMatrix = wrapper.Documents.FirstOrDefault(doc =>
            doc.Type == "T" && doc.Id.Split('/')[3] == classCode.ToString());

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

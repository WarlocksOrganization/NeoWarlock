using System;
using System.IO;
using UnityEngine;

public static class MatrixFileManager
{
    private static readonly string MatrixFileName = "CardMatrixList.json";

    public static string GetMatrixFilePath()
    {
        return Path.Combine(Application.persistentDataPath, MatrixFileName);
    }

    public static void SaveMatrixJson(string jsonText)
    {
        string path = GetMatrixFilePath();
        File.WriteAllText(path, jsonText);
        Debug.Log($"[MatrixFileManager] 매트릭스 JSON 저장 완료: {path}");
    }
    
    public static string LoadMatrixJson()
    {
        try
        {
            string path = GetMatrixFilePath();
            if (!File.Exists(path))
            {
                Debug.LogWarning("[MatrixFileManager] 매트릭스 파일이 존재하지 않습니다.");
                return null;
            }
            return File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MatrixFileManager] 파일 로드 실패: {e.Message}");
            return null;
        }
    }

    public static bool HasMatrixFile()
    {
        return File.Exists(GetMatrixFilePath());
    }
}

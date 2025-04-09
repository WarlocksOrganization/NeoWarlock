using UnityEngine;
using System;

public static class MatrixUpdateTracker
{
    private const string Key = "LastMatrixUpdate";

    public static void SaveNow()
    {
        PlayerPrefs.SetString(Key, DateTime.UtcNow.ToString("o"));
    }

    public static bool ShouldUpdateAfterHours(int hours)
    {
        if (!PlayerPrefs.HasKey(Key)) return true;

        DateTime last = DateTime.Parse(PlayerPrefs.GetString(Key));
        return (DateTime.UtcNow - last).TotalHours > hours;
    }
}
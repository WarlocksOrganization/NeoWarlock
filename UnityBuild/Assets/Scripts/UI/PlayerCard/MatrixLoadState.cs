using UnityEngine;

public static class MatrixLoadState
{
    public static bool HasMatrixData { get; set; } = false;
    public static string LastLoadSource { get; set; } = "None"; // "Persistent", "None"
}

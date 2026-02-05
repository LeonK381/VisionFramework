/// <summary>
/// Static container for test results (simple approach for demo).
/// In production you might want serializable ScriptableObject or JSON persistence.
/// </summary>
public static class AstigmatismResult
{
    public static float axis = 0f;        // degrees (0..180)
    public static float strength = 0f;    // 0..1
    public static float previewBlur = 0f; // runtime preview value (optional)
}


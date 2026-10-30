
using UnityEngine;

public class FPSMonitor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private int fontSize = 24;
    [SerializeField] private int margin = 15;

    private static FPSMonitor instance;

    private float elapsedTime;
    private int frameCount;
    private string fpsText = "FPS: --";

    private GUIStyle style;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        elapsedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (elapsedTime >= updateInterval)
        {
            float fps = frameCount / elapsedTime;
            float ms = (elapsedTime / frameCount) * 1000f;

            fpsText = $"{fps:F0} FPS  |  {ms:F1} ms";

            frameCount = 0;
            elapsedTime = 0f;
        }
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = fontSize;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperRight;
        }

        float width = 280f;
        float height = 40f;

        Rect rect = new Rect(
            Screen.width - width - margin,
            margin,
            width,
            height
        );

        // Shadow
        style.normal.textColor = Color.black;

        GUI.Label(
            new Rect(rect.x + 1, rect.y + 1, width, height),
            fpsText,
            style
        );

        // FPS color
        float fps = frameCount > 0 && elapsedTime > 0
            ? frameCount / elapsedTime
            : 60f;

        style.normal.textColor =
            fps >= 55 ? Color.green :
            fps >= 30 ? Color.yellow :
            Color.red;

        GUI.Label(rect, fpsText, style);
    }
}

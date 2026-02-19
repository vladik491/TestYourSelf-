using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ForceLeftClick : MonoBehaviour
{
    public static ForceLeftClick Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyInputFix();
    }

    void Start()
    {
        ApplyInputFix();
    }

    public void ApplyInputFix()
    {
        EventSystem es = EventSystem.current;
        if (es != null)
        {
            StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();

            if (oldModule != null && !(oldModule is CustomLeftClickModule))
            {
                Destroy(oldModule);
                es.gameObject.AddComponent<CustomLeftClickModule>();
            }
        }
    }
}

public class CustomLeftClickModule : StandaloneInputModule
{
    public override void Process()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) ||
            Input.GetMouseButton(1) || Input.GetMouseButton(2) ||
            Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        {
            return;
        }

        base.Process();
    }
}
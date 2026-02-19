using UnityEngine;
using YG;

public class GameInitializer : MonoBehaviour
{
    [Header("Компонент для управления лидербордом")]
    public LeaderboardManager leaderboardManager;

    void Start()
    {
        if (YG2.isSDKEnabled)
        {
            OnYandexGameInitialized();
        }
        else
        {
            YG2.onGetSDKData += OnYandexGameInitialized;
        }
    }

    void OnDestroy()
    {
        YG2.onGetSDKData -= OnYandexGameInitialized;
    }

    private void OnYandexGameInitialized()
    {
        // 1. Сначала инициализируем очки (загрузка сохранений)
        // DailyRunTracker сам вызовет PointsManager.Init(), но для надежности можно и тут, 
        // так как в PointsManager есть защита от двойной инициализации (чтение переменной).
        PointsManager.Init();

        // 2. Включаем менеджер лидерборда
        if (leaderboardManager != null)
        {
            leaderboardManager.gameObject.SetActive(true);
            // Принудительно запрашиваем данные лидерборда при старте
            leaderboardManager.RequestLeaderboardData();
        }
        else
        {
            // Поиск компонента, если забыли привязать
            var lbManager = FindFirstObjectByType<LeaderboardManager>();
            if (lbManager != null)
            {
                lbManager.enabled = true;
                lbManager.RequestLeaderboardData();
            }
        }
    }
}
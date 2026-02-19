using UnityEngine;
using YG; 
using YG.Utils.LB; // Нужно для работы с данными лидерборда (LBData)

public class LeaderboardManager : MonoBehaviour
{
    [Header("Настройки Лидерборда")]
    public string leaderboardTechnoName = "ContentPoints";

    private float lastSendTime = -999f;
    private const float SEND_COOLDOWN = 1.1f; // Ограничение "не чаще раз в секунду"

    // Храним лучший счет, который УЖЕ есть в облаке Яндекса
    private int bestScoreInCloud = 0;
    // Флаг, получили ли мы данные от Яндекса
    private bool isLeaderboardInitialized = false;

    void OnEnable()
    {
        // Подписываемся на изменение очков
        PointsManager.OnPointsChanged += HandlePointsChanged;
        // Подписываемся на получение данных лидерборда (ответ от сервера)
        YG2.onGetLeaderboard += OnLeaderboardDataReceived;

        // Если SDK уже инициализирован, сразу пробуем получить данные лидерборда
        if (YG2.isSDKEnabled)
        {
            RequestLeaderboardData();
        }
    }

    void OnDisable()
    {
        PointsManager.OnPointsChanged -= HandlePointsChanged;
        YG2.onGetLeaderboard -= OnLeaderboardDataReceived;
    }

    // 1. Запрашиваем текущее состояние таблицы, чтобы узнать реальный рекорд игрока
    public void RequestLeaderboardData()
    {
        if (YG2.isSDKEnabled && !string.IsNullOrEmpty(leaderboardTechnoName))
        {
            // Запрашиваем данные, чтобы узнать текущий рекорд (Score) игрока
            YG2.GetLeaderboard(leaderboardTechnoName);
        }
    }

    // 2. Обработка ответа от Яндекса
    private void OnLeaderboardDataReceived(LBData data)
    {
        // Проверяем, что пришли данные именно от нашей таблицы
        if (data.technoName == leaderboardTechnoName)
        {
            // Получаем текущий рекорд игрока из облака
            if (data.currentPlayer != null)
            {
                bestScoreInCloud = data.currentPlayer.score;
            }
            else
            {
                bestScoreInCloud = 0; // Игрока нет в таблице
            }

            isLeaderboardInitialized = true;

            // Сразу после инициализации проверяем: вдруг локальные очки больше, чем в облаке?
            // Это решает проблему "я не в лидерборде", если очки были набраны оффлайн
            CheckAndSync(PointsManager.GetTotalPoints());
        }
    }

    // 3. Реакция на изменение очков в игре
    private void HandlePointsChanged(int currentTotalPoints)
    {
        CheckAndSync(currentTotalPoints);
    }

    // 4. Логика сравнения и отправки (защита от перезаписи меньшим числом)
    private void CheckAndSync(int currentScore)
    {
        // Если мы еще не знаем, какой рекорд в облаке, лучше не отправлять (чтобы не стереть),
        // либо запрашиваем данные снова.
        if (!isLeaderboardInitialized)
        {
            RequestLeaderboardData();
            return;
        }

        // ВАЖНО: Отправляем только если новый счет СТРОГО БОЛЬШЕ того, что уже в Яндексе
        if (currentScore > bestScoreInCloud)
        {
            TrySendScore(currentScore);
        }
    }

    private void TrySendScore(int score)
    {
        if (!YG2.isSDKEnabled) return;

        if (Time.time < lastSendTime + SEND_COOLDOWN)
        {
            // Если слишком часто шлем, можно пропустить, 
            // но лучше запланировать повторную отправку (упрощенно - просто выходим)
            return;
        }

        if (string.IsNullOrEmpty(leaderboardTechnoName) || leaderboardTechnoName == "YourTechnoNameHere")
        {
            Debug.LogError("LeaderboardManager: Не указано техническое имя таблицы!");
            return;
        }

        Debug.Log($"LeaderboardManager: Новый рекорд! Отправляем {score} (Старый был: {bestScoreInCloud})");

        // Обновляем локальное понимание рекорда, чтобы не спамить запросами
        bestScoreInCloud = score;

        YG2.SetLeaderboard(leaderboardTechnoName, score);
        lastSendTime = Time.time;
    }
}
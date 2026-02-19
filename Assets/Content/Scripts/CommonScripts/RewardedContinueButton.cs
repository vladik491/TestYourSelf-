using UnityEngine;
using UnityEngine.UI;
using YG;
using System.Collections;
using System;

public class RewardedContinueButton : MonoBehaviour
{
    public enum LevelTarget { Easy, Medium, Hard }

    [Header("Target")]
    [SerializeField] private LevelTarget target;

    [Header("Managers (assign in inspector)")]
    [SerializeField] private EasyLevelManager easyLevelManager;
    [SerializeField] private MediumLevelManager mediumLevelManager;
    [SerializeField] private HardLevelManager hardLevelManager;
    [SerializeField] private GeneralLevelManager generalLevelManager;
    [SerializeField] private BunchLevelManager bunchLevelManager;

    [Header("UI")]
    [SerializeField] private Button adButton;
    [SerializeField] private string rewardID;

    private Coroutine _adTimeoutCoroutine;
    private const float AD_TIMEOUT_SECONDS = 8f;

    public void OnButtonDown()
    {
        if (adButton == null) return;
        var rt = adButton.GetComponent<RectTransform>();
        if (rt != null) rt.localScale = new Vector3(0.95f, 0.95f, 1f);
    }

    public void OnButtonUp()
    {
        if (adButton == null) return;
        var rt = adButton.GetComponent<RectTransform>();
        if (rt != null) rt.localScale = Vector3.one;
    }

    public void OnAdButtonPressed()
    {
        // 1. ПРОВЕРКА ГЛОБАЛЬНОЙ БЛОКИРОВКИ:
        // Проверяем, не начал ли другой скрипт (например, ReturnToSelectionButton) критическое действие
        if (ReturnToSelectionButton.IsCriticalActionInProgress)
        {
            Debug.LogWarning("Критическая операция уже выполняется. Игнорируем нажатие RewardedAd.");
            return;
        }

        // 2. БЛОКИРОВКА: Устанавливаем глобальный флаг, чтобы предотвратить параллельное нажатие на кнопку возврата
        ReturnToSelectionButton.IsCriticalActionInProgress = true;

        // 3. Убеждаемся, что игра точно не на паузе, если вдруг была
        Time.timeScale = 1f;

        // Отключаем кнопку, пока идет реклама
        if (adButton != null) adButton.interactable = false;

        // safety timeout: если колбэк не придет — восстановим кнопку через AD_TIMEOUT_SECONDS
        if (_adTimeoutCoroutine != null) StopCoroutine(_adTimeoutCoroutine);
        _adTimeoutCoroutine = StartCoroutine(AdButtonTimeout());

        Action adSuccessCallback = () =>
        {
            // !!! САМОЕ ВАЖНОЕ ИЗМЕНЕНИЕ 1: БЕЗОПАСНЫЙ ВЫХОД, ЕСЛИ ОБЪЕКТ УНИЧТОЖЕН !!!
            // Проверка this == null предотвращает MissingReferenceException
            if (this == null)
            {
                Debug.LogWarning("RewardedContinueButton был уничтожен до завершения рекламы. MissingReferenceException предотвращена.");
                // Глобальную блокировку не сбрасываем, так как другой скрипт, вероятно, уже начал переход сцены.
                return;
            }

            // отменяем таймаут (если он всё ещё работает)
            if (_adTimeoutCoroutine != null) { StopCoroutine(_adTimeoutCoroutine); _adTimeoutCoroutine = null; }

            // 4. СБРОС БЛОКИРОВКИ
            ReturnToSelectionButton.IsCriticalActionInProgress = false;

            // 5. ВРУЧНУЮ ВОССТАНАВЛИВАЕМ ВРЕМЯ
            Time.timeScale = 1f;

            Debug.Log("YG2 Callback: TimeScale сброшен на 1. Вызов GrantContinue.");

            GrantContinue();

            // Включаем кнопку
            if (adButton != null) adButton.interactable = true;
        };

        YG2.RewardedAdvShow(rewardID, adSuccessCallback);
    }

    private IEnumerator AdButtonTimeout()
    {
        yield return new WaitForSeconds(AD_TIMEOUT_SECONDS);

        // 1. БЕЗОПАСНЫЙ ВЫХОД, ЕСЛИ ОБЪЕКТ УНИЧТОЖЕН
        if (this == null)
        {
            yield break;
        }

        // 2. СБРОС БЛОКИРОВКИ: Если таймаут сработал, значит, реклама не показалась/не завершилась, 
        // и нужно разблокировать UI, чтобы пользователь мог нажать снова.
        ReturnToSelectionButton.IsCriticalActionInProgress = false;

        if (adButton != null) adButton.interactable = true;

        // !!! Дополнительно: сброс TimeScale после таймаута (если он сработал)
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        _adTimeoutCoroutine = null;
    }

    private void GrantContinue()
    {
        Debug.Log("GrantContinue вызван.");

        // Сначала проверяем BunchLevelManager
        if (bunchLevelManager != null)
        {
            bunchLevelManager.ContinueFromAdReward();
            return;
        }

        // затем проверяем общий менеджер
        if (generalLevelManager != null)
        {
            generalLevelManager.ContinueFromAdReward();
            return;
        }

        // иначе вызываем конкретный менеджер в соответствии с target
        switch (target)
        {
            case LevelTarget.Easy:
                if (easyLevelManager != null) easyLevelManager.ContinueFromAdReward();
                break;
            case LevelTarget.Medium:
                if (mediumLevelManager != null) mediumLevelManager.ContinueFromAdReward();
                break;
            case LevelTarget.Hard:
                if (hardLevelManager != null) hardLevelManager.ContinueFromAdReward();
                break;
        }
    }
}
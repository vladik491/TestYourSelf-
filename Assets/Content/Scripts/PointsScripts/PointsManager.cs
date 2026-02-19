using System;
using UnityEngine;
using YG; 
using PlayerPrefs = RedefineYG.PlayerPrefs;

public static class PointsManager
{
    private const string Key = "TotalPoints";
    public static event Action<int> OnPointsChanged;

    private static int _cachedTotal = 0;

    public static void Init()
    {
        _cachedTotal = PlayerPrefs.GetInt(Key, 0);
    }

    public static int GetTotalPoints()
    {
        return _cachedTotal;
    }

    public static void AddPoints(int amount)
    {
        if (amount <= 0) return;

        _cachedTotal += amount;

        SaveToStorage();

        OnPointsChanged?.Invoke(_cachedTotal);
    }

    public static void ResetPoints()
    {
        _cachedTotal = 0;

        SaveToStorage();

        OnPointsChanged?.Invoke(_cachedTotal);
    }

    private static void SaveToStorage()
    {
        try
        {
            PlayerPrefs.SetInt(Key, _cachedTotal);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"PointsManager.SaveToStorage(): Не удалось сохранить! Ошибка: {e.Message}");
        }
    }
}
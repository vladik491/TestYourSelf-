using System.Collections.Generic;
using UnityEngine;

public enum Gender { Male, Female, Other }

[System.Serializable]
public class SurnameBlock
{
    [Tooltip("—писок фамилий/вариантов фамилий дл€ персонажа")]
    public List<string> surnames = new List<string>();
}

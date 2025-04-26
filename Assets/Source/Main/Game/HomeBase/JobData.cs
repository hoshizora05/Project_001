using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Job/Job Data")]
public class JobData : ScriptableObject
{
    [Header("表示情報")]
    public string jobName;
    [TextArea] public string jobDescription;

    [Header("消費と報酬")]
    public float requiredStamina;
    public int rewardMoney;
    public string rewardItemKey;

    [Header("成功判定")]
    public float baseSuccessRate = 0.5f;
    public float recommendedStamina = 50f;
    public string mainParameterKey;
    public float mainParameterGain;
    public float mainParameterInfluence = 1.0f;

    [Header("イベント")]
    public float eventTriggerProbability = 0.0f;
    public string eventID;

    /// <summary>
    /// 説明文をUI用に整形して返す。
    /// </summary>
    public string GetDescription()
    {
        string result = jobDescription + "\n";
        result += $"報酬: ${rewardMoney}\n";
        result += $"{mainParameterKey}+{mainParameterGain}\n";
        if (!string.IsNullOrEmpty(rewardItemKey))
            result += $"アイテム: {rewardItemKey}\n";
        return result;
    }
}

[Serializable]
public class ParameterGain
{
    public string statId;
    public float amount;
}
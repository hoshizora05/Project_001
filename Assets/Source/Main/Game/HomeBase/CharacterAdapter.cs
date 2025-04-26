using System;
using System.Collections.Generic;
using UnityEngine;
using CharacterSystem;
using SocialActivity;
using System.Linq;
using ProgressionAndEventSystem;

// CharacterSystem.CharacterManager.Character を SocialActivity.ICharacter および CharacterSystem.ICharacter にアダプトするクラス
public class CharacterAdapter : SocialActivity.ICharacter, CharacterSystem.ICharacter,ProgressionAndEventSystem.ICharacter
{
    private CharacterManager.Character _character;

    public CharacterAdapter(CharacterManager.Character character)
    {
        _character = character;
    }

    // SocialActivity.ICharacter インターフェースの実装
    public string Name => _character.baseInfo.name;

    // 実際のゲームデータに応じてスキルをマッピング
    public Dictionary<PlayerSkill, float> Skills { get; } = new Dictionary<PlayerSkill, float>();

    // キャラクターの特性をマッピング
    public Dictionary<string, float> Traits { get; } = new Dictionary<string, float>();

    // スケジュールを保持
    public List<ScheduledActivity> Schedule { get; } = new List<ScheduledActivity>();

    public void ModifyRelationship(SocialActivity.ICharacter target, SocialActivity.RelationshipParameter parameter, float amount)
    {
        // RelationshipNetwork等を使用して関係を変更
        // 例：RelationshipNetwork.Instance.ModifyRelationship(_character.baseInfo.characterId, target.Name, amount);
    }

    public float GetRelationshipValue(SocialActivity.ICharacter target, SocialActivity.RelationshipParameter parameter)
    {
        // RelationshipNetworkから関係値を取得
        // 例：return RelationshipNetwork.Instance.GetRelationshipValue(_character.baseInfo.characterId, target.Name);
        return 0f; // 仮の実装
    }

    public void AddMemory(MemoryRecord memory)
    {
        // キャラクターのメモリーシステムに追加
        // 実際のゲームの記憶システムに応じて実装
    }

    public bool HasRequiredItems(List<string> requiredItems)
    {
        // インベントリシステムを使用してアイテム所持を確認
        // 例：InventorySystem.HasItems(_character.baseInfo.characterId, requiredItems);
        return false; // 仮の実装
    }

    // ------------------  CharacterSystem.ICharacter  ------------------
    public string CharacterId => _character.baseInfo.characterId;
    public string CharacterName => _character.baseInfo.name;
    public Dictionary<SkillType, float> CharacterSkills => ConvertSkills();
    public Dictionary<AttributeType, float> CharacterAttributes => ConvertAttributes();

    public MentalState.EmotionalState CharacterCurrentEmotionalState => GetCurrentEmotion();

    public Dictionary<PreferenceType, float> CharacterPreferences => new();
    public CharacterSystem.LocationType CharacterCurrentLocation => CharacterSystem.LocationType.Other;
    public CharacterSystem.Schedule CharacterDailySchedule => new()
    {
        DailyLocations = new(),
        SpecialSchedules = new()
    };


    // ----------- ProgressionAndEventSystem.ICharacter -----------
    public string Id => _character.baseInfo.characterId;

    public Dictionary<string, float> GetStats()
    {
        return new Dictionary<string, float>
        {
            { "strength", 5f },
            { "intelligence", 6f },
            { "charisma", 4f }
        };
    }

    public Dictionary<string, object> GetState()
    {
        return new Dictionary<string, object>
        {
            { "isPregnant", false },
            { "mood", "happy" }
        };
    }

    public Dictionary<string, float> GetRelationships()
    {
        return new Dictionary<string, float>
        {
            { "npc_001", 50f },
            { "npc_002", 30f }
        };
    }

    public string GetCurrentLocation()
    {
        return "start_area"; // 仮実装
    }

    private Dictionary<string, bool> _flags = new();
    public bool HasFlag(string flagName) => _flags.TryGetValue(flagName, out var value) && value;
    public void SetFlag(string flagName, bool value) => _flags[flagName] = value;

    private Dictionary<string, DateTime> _eventHistory = new();
    public Dictionary<string, DateTime> GetEventHistory() => _eventHistory;
    public void RecordEventOccurrence(string eventId) => _eventHistory[eventId] = DateTime.Now;


    // ------------------  Helper Methods  ------------------
    private MentalState.EmotionalState GetCurrentEmotion()
    {
        var ms = _character.mentalState;
        if (ms == null || ms.emotionalStates == null || ms.emotionalStates.Count == 0)
        {
            // デフォルトのニュートラル感情
            return new MentalState.EmotionalState
            {
                type = "Neutral",
                currentValue = 0f,
                volatility = 0.5f,
                decayRate = 0.1f
            };
        }

        // 絶対値が最も大きい（＝最も優勢な）感情を返す
        return ms.emotionalStates
                 .OrderByDescending(e => Mathf.Abs(e.currentValue))
                 .First();
    }

    // ヘルパーメソッド - スキルのコンバート
    private Dictionary<CharacterSystem.SkillType, float> ConvertSkills()
    {
        // 実際のゲームデータに応じてスキルをマッピング
        Dictionary<CharacterSystem.SkillType, float> skills = new Dictionary<CharacterSystem.SkillType, float>();

        // 仮のデータを設定
        foreach (CharacterSystem.SkillType skill in Enum.GetValues(typeof(CharacterSystem.SkillType)))
        {
            skills[skill] = 5.0f; // デフォルト値
        }

        return skills;
    }

    // ヘルパーメソッド - 属性のコンバート
    private Dictionary<CharacterSystem.AttributeType, float> ConvertAttributes()
    {
        // 実際のゲームデータに応じて属性をマッピング
        Dictionary<CharacterSystem.AttributeType, float> attributes = new Dictionary<CharacterSystem.AttributeType, float>();

        // 仮のデータを設定
        foreach (CharacterSystem.AttributeType attribute in Enum.GetValues(typeof(CharacterSystem.AttributeType)))
        {
            attributes[attribute] = 5.0f; // デフォルト値
        }

        return attributes;
    }
}
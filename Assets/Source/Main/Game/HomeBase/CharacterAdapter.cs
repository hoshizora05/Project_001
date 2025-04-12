using System;
using System.Collections.Generic;
using UnityEngine;
using CharacterSystem;
using SocialActivity;
using System.Linq;

// CharacterSystem.CharacterManager.Character を SocialActivity.ICharacter および CharacterSystem.ICharacter にアダプトするクラス
public class CharacterAdapter : SocialActivity.ICharacter, CharacterSystem.ICharacter
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
    string CharacterSystem.ICharacter.Id => _character.baseInfo.characterId;
    string CharacterSystem.ICharacter.Name => _character.baseInfo.name;
    Dictionary<SkillType, float> CharacterSystem.ICharacter.Skills => ConvertSkills();
    Dictionary<AttributeType, float> CharacterSystem.ICharacter.Attributes => ConvertAttributes();

    MentalState.EmotionalState CharacterSystem.ICharacter.CurrentEmotionalState => GetCurrentEmotion();

    Dictionary<PreferenceType, float> CharacterSystem.ICharacter.Preferences => new();
    CharacterSystem.LocationType CharacterSystem.ICharacter.CurrentLocation => CharacterSystem.LocationType.Other;
    CharacterSystem.Schedule CharacterSystem.ICharacter.DailySchedule => new()
    {
        DailyLocations = new(),
        SpecialSchedules = new()
    };

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
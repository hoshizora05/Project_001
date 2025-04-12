using System;
using System.Collections.Generic;
using UnityEngine;
namespace CharacterSystem
{
    [CreateAssetMenu(fileName = "DialogueDatabase", menuName = "MyGame/Dialogue Database")]
    public class DialogueDatabase : ScriptableObject
    {
        public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

        /// <summary>
        /// 指定した conversationId に合致する DialogueEntry を全て取得する。
        /// </summary>
        public List<DialogueEntry> GetEntriesByConversationId(string conversationId)
        {
            List<DialogueEntry> results = new List<DialogueEntry>();
            foreach (DialogueEntry entry in dialogueEntries)
            {
                // conversationId が完全一致するかチェック (大文字小文字を区別しない)
                if (entry.conversationId.Equals(conversationId, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(entry);
                }
            }
            return results;
        }

        /// <summary>
        /// （例）Topicベースで会話の選択肢を取得する既存メソッド
        /// </summary>
        public List<DialogueChoice> GetChoicesMatchingContext(
            DialogueTopic topic, DialogueContext context, ICharacter player, ICharacter npc)
        {
            List<DialogueChoice> result = new List<DialogueChoice>();

            foreach (DialogueEntry entry in dialogueEntries)
            {
                if (entry.topic == topic)
                {
                    result.AddRange(entry.choices);
                }
            }
            return result;
        }

        /// <summary>
        /// （例）TopicベースでNPCの台詞を返す既存メソッド
        /// </summary>
        public string GetNpcLine(DialogueTopic topic, DialogueContext context, ICharacter player, ICharacter npc)
        {
            List<DialogueEntry> matchingEntries = new List<DialogueEntry>();
            foreach (var entry in dialogueEntries)
            {
                if (entry.topic == topic)
                {
                    matchingEntries.Add(entry);
                }
            }

            if (matchingEntries.Count == 0)
            {
                return "…(No data for this topic)…";
            }

            int index = UnityEngine.Random.Range(0, matchingEntries.Count);
            var chosenEntry = matchingEntries[index];

            if (chosenEntry.npcLines != null && chosenEntry.npcLines.Count > 0)
            {
                int lineIndex = UnityEngine.Random.Range(0, chosenEntry.npcLines.Count);
                return chosenEntry.npcLines[lineIndex];
            }
            else
            {
                return "…(Entry has no lines)…";
            }
        }
    }

    [Serializable]
    public class DialogueEntry
    {
        [Tooltip("自由に設定できる会話ID。例: 'Introduction' / 'Quest01' / 'MyEvent' など")]
        public string conversationId = "DefaultConversation";

        public DialogueTopic topic;

        [Tooltip("NPCが話す台詞候補")]
        public List<string> npcLines = new List<string>();

        [Tooltip("このエントリでプレイヤーが選べる選択肢群")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }
}
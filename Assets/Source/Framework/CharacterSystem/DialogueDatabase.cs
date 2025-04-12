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
        /// �w�肵�� conversationId �ɍ��v���� DialogueEntry ��S�Ď擾����B
        /// </summary>
        public List<DialogueEntry> GetEntriesByConversationId(string conversationId)
        {
            List<DialogueEntry> results = new List<DialogueEntry>();
            foreach (DialogueEntry entry in dialogueEntries)
            {
                // conversationId �����S��v���邩�`�F�b�N (�啶������������ʂ��Ȃ�)
                if (entry.conversationId.Equals(conversationId, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(entry);
                }
            }
            return results;
        }

        /// <summary>
        /// �i��jTopic�x�[�X�ŉ�b�̑I�������擾����������\�b�h
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
        /// �i��jTopic�x�[�X��NPC�̑䎌��Ԃ��������\�b�h
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
                return "�c(No data for this topic)�c";
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
                return "�c(Entry has no lines)�c";
            }
        }
    }

    [Serializable]
    public class DialogueEntry
    {
        [Tooltip("���R�ɐݒ�ł����bID�B��: 'Introduction' / 'Quest01' / 'MyEvent' �Ȃ�")]
        public string conversationId = "DefaultConversation";

        public DialogueTopic topic;

        [Tooltip("NPC���b���䎌���")]
        public List<string> npcLines = new List<string>();

        [Tooltip("���̃G���g���Ńv���C���[���I�ׂ�I�����Q")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }
}
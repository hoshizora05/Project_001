using PlayerProgression.Data;
using PlayerProgression.Interfaces;

namespace PlayerProgression.Commands
{
    public class ModifyStatCommand : IProgressionCommand
    {
        private readonly IStatSystem statSystem;
        private readonly string statId;
        private readonly PlayerStats.StatModifier modifier;
        private readonly bool isTemporary;
        
        public ModifyStatCommand(IStatSystem system, string id, PlayerStats.StatModifier mod, bool temporary = true)
        {
            statSystem = system;
            statId = id;
            modifier = mod;
            isTemporary = temporary;
        }
        
        public void Execute()
        {
            statSystem.ApplyModifier(statId, modifier);
        }
        
        public void Undo()
        {
            if (isTemporary)
            {
                statSystem.RemoveModifiersFromSource(statId, modifier.source);
            }
        }
    }
}
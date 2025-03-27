using PlayerProgression.Data;
using PlayerProgression.Interfaces;

namespace PlayerProgression.Commands
{
    public class AddReputationEventCommand : IProgressionCommand
    {
        private readonly IReputationSystem reputationSystem;
        private readonly string contextId;
        private readonly SocialStandingSystem.ReputationEvent reputationEvent;
        
        public AddReputationEventCommand(IReputationSystem system, string context, SocialStandingSystem.ReputationEvent evt)
        {
            reputationSystem = system;
            contextId = context;
            reputationEvent = evt;
        }
        
        public void Execute()
        {
            reputationSystem.AddReputationEvent(contextId, reputationEvent);
        }
        
        public void Undo()
        {
            // Reputation events are harder to undo perfectly
            // A proper implementation would need to store the previous reputation state
        }
    }
}
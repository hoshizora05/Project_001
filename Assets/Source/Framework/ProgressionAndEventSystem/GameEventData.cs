using UnityEngine;
namespace ProgressionAndEventSystem
{
    [CreateAssetMenu(fileName = "NewEvent", menuName = "Events/Game Event")]
    public class GameEventData : ScriptableObject
    {
        public GameEvent eventData;

        /// <summary>
        /// Return a deep copy of the event data to prevent mutation of the original ScriptableObject.
        /// </summary>
        public GameEvent GetGameEvent()
        {
            return eventData?.DeepClone();
        }
    }
}
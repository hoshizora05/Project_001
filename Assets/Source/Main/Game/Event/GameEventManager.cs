using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProgressionAndEventSystem;

/// <summary>
/// Game‑level implementation of <see cref="IEventManager"/> that fully supports Dialogue flow and
/// ProgressConditions contained in <see cref="EventStage"/>.  
/// ‑ UI 連携: <b>OnChoicesRequired</b>/<b>OnProgressUpdated</b> を利用してダイアログ・進捗を都度通知  
/// ‑ Dialogue: 各 <see cref="DialogueLine"/> を時間管理し順次再生  
/// ‑ ProgressConditions: 条件が満たされ次第ステージを自動遷移  
/// ‑ MakeChoice / CancelEvent / UpdateEventStates 完全実装  
/// </summary>
[AddComponentMenu("Game/Systems/Game Event Manager (Full)")]
public sealed class GameEventManager : MonoBehaviour, IEventManager
{
    #region ░░ Public events ░░
    public event Action<GameEvent> OnEventRegistered;
    public event Action<GameEvent, ICharacter> OnEventTriggered;
    public event Action<GameEvent, EventResult, ICharacter> OnEventCompleted;
    public event Action<GameEvent, List<EventChoice>, ICharacter> OnChoicesRequired;
    public event Action<GameEvent, float, string, ICharacter> OnProgressUpdated;
    #endregion

    #region ░░ Serialized refs ░░
    [Header("Required sub‑systems")]
    [SerializeField] private MonoBehaviour _effectSystemBehaviour; // implements IEventEffectSystem
    [SerializeField] private MonoBehaviour _typeSystemBehaviour;   // implements IEventTypeSystem

    private IEventEffectSystem EffectSystem => (IEventEffectSystem)_effectSystemBehaviour;
    private IEventTypeSystem TypeSystem => (IEventTypeSystem)_typeSystemBehaviour;
    #endregion

    #region ░░ Internal state ░░
    private readonly Dictionary<string, GameEvent> _registered = new();
    private readonly Dictionary<string, ActiveEvent> _active = new(); // key=eventId
    private readonly Dictionary<string, DateTime> _lastRun = new(); // cooldown tracking

    private sealed class ActiveEvent
    {
        public ICharacter Player;
        public GameEvent Data;
        public EventStage CurrentStage;
        public DateTime StageEnterTime;
        public float TimeLimitProgress;   // 0‑1 (time‑based)

        // Dialogue flow
        public int DialogueIndex;
        public DateTime DialogueStartTime;
        public bool DialogueRunning;

        public bool Completed;
    }
    #endregion

    #region ░░ Unity ░░
    private void Awake()
    {
        if (EffectSystem == null) Debug.LogError("[GameEventManager] IEventEffectSystem missing.");
        if (TypeSystem == null) Debug.LogError("[GameEventManager] IEventTypeSystem   missing.");
    }

    private void Update() => UpdateEventStates();
    #endregion

    #region ░░ IEventManager implementation ░░
    public void RegisterEvent(GameEvent gameEvent)
    {
        if (gameEvent == null || string.IsNullOrEmpty(gameEvent.Id))
        {
            Debug.LogError("[GameEventManager] Invalid event registration.");
            return;
        }
        _registered[gameEvent.Id] = gameEvent;
        TypeSystem.RegisterEventType(gameEvent.Type);
        OnEventRegistered?.Invoke(gameEvent);
    }

    public void UnregisterEvent(string eventId) => _registered.Remove(eventId);

    public List<GameEvent> GetAvailableEvents(ICharacter player)
    {
        List<GameEvent> list = new();
        foreach (var ev in _registered.Values)
            if (IsEventAvailable(ev, player)) list.Add(ev);
        list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return list;
    }

    public void TriggerEvent(string eventId, ICharacter player)
    {
        if (!_registered.TryGetValue(eventId, out var ev))
        {
            Debug.LogWarning($"[GameEventManager] Unknown eventId {eventId}");
            return;
        }
        if (!IsEventAvailable(ev, player))
        {
            Debug.LogWarning($"[GameEventManager] Event {eventId} not available.");
            return;
        }

        var active = new ActiveEvent
        {
            Player = player,
            Data = ev,
            CurrentStage = ev.Stages.FirstOrDefault(),
            StageEnterTime = DateTime.Now,
            DialogueIndex = 0,
            DialogueStartTime = DateTime.Now,
            DialogueRunning = false,
            TimeLimitProgress = 0f,
            Completed = false
        };
        _active[eventId] = active;
        _lastRun[eventId] = DateTime.Now;
        player.RecordEventOccurrence(eventId);
        OnEventTriggered?.Invoke(ev, player);
        EnterStage(active);
    }

    public bool CheckEventConditions(GameEvent ev, ICharacter player)
    {
        if (ev.Conditions == null || ev.Conditions.Count == 0) return true;
        return EvaluateConditions(ev.Conditions, player);
    }

    public void UpdateEventStates()
    {
        if (_active.Count == 0) return;
        List<string> finished = new();

        foreach (var kv in _active)
        {
            var ac = kv.Value;
            var stage = ac.CurrentStage;
            if (stage == null) continue;

            // ── Dialogue playback ───────────────────────────────
            if (ac.DialogueRunning)
            {
                var dlg = stage.Dialogue[ac.DialogueIndex];
                if ((DateTime.Now - ac.DialogueStartTime).TotalSeconds >= dlg.Duration)
                {
                    ac.DialogueIndex++;
                    if (ac.DialogueIndex < stage.Dialogue.Count)
                    {
                        // 次の行を送信
                        SendDialogueLine(ac);
                    }
                    else
                    {
                        ac.DialogueRunning = false;
                        // Dialogue 終了 → Choices か ProgressConditions
                        if (stage.Choices != null && stage.Choices.Count > 0)
                            OnChoicesRequired?.Invoke(ac.Data, stage.Choices, ac.Player);
                    }
                }
                continue; // still in dialogue
            }

            // ── Stage TimeLimit progress (if any) ───────────────
            if (stage.TimeLimit.HasValue)
            {
                float ratio = Mathf.Clamp01((float)(DateTime.Now - ac.StageEnterTime).TotalSeconds / (float)stage.TimeLimit.Value.TotalSeconds);
                if (!Mathf.Approximately(ratio, ac.TimeLimitProgress))
                {
                    ac.TimeLimitProgress = ratio;
                    OnProgressUpdated?.Invoke(ac.Data, ratio, stage.Description, ac.Player);
                }
                if (ratio >= 1f)
                {
                    AutoFailStage(ac);
                    continue;
                }
            }

            // ── ProgressConditions check ───────────────────────
            if (stage.ProgressConditions != null && stage.ProgressConditions.Count > 0)
            {
                if (EvaluateConditions(stage.ProgressConditions, ac.Player))
                {
                    AdvanceToNextStage(ac, stage.NextStageId);
                    continue;
                }
            }
        }

        // cleanup (safety – FinishEvent already removes)
        foreach (var id in finished)
            _active.Remove(id);
    }

    public void MakeChoice(GameEvent ev, EventChoice choice, ICharacter player)
    {
        if (!_active.TryGetValue(ev.Id, out var ac))
        {
            Debug.LogWarning("[GameEventManager] MakeChoice on inactive event.");
            return;
        }

        // Apply choice effects via EffectSystem
        if (EffectSystem != null && choice.Effects != null && choice.Effects.Count > 0)
        {
            var fakeResult = new EventResult { AppliedEffects = choice.Effects };
            EffectSystem.ApplyEventEffects(ev, fakeResult, player);
        }

        string nextId = ac.CurrentStage.ConditionalNextStages != null &&
                         ac.CurrentStage.ConditionalNextStages.TryGetValue(choice.Id, out var cNext)
                         ? cNext
                         : ac.CurrentStage.NextStageId;

        // Record choice
        ac.Data.EventData ??= new Dictionary<string, object>();
        ac.Data.EventData[$"choice_{ac.CurrentStage.StageId}"] = choice.Id;

        if (string.IsNullOrEmpty(nextId))
        {
            // Event ends here
            var res = BuildEventResult(ev, player, true);
            FinishEvent(ac, res);
        }
        else
        {
            AdvanceToNextStage(ac, nextId);
        }
    }

    public void CancelEvent(string eventId, ICharacter player)
    {
        if (!_active.TryGetValue(eventId, out var ac)) return;
        EffectSystem?.ReverseEventEffects(ac.Data, player);
        _active.Remove(eventId);
    }
    #endregion

    #region ░░ Stage management ░░
    private void EnterStage(ActiveEvent ac)
    {
        var stage = ac.CurrentStage;
        if (stage == null) { FinishEvent(ac, BuildEventResult(ac.Data, ac.Player, false)); return; }

        ac.StageEnterTime = DateTime.Now;
        ac.TimeLimitProgress = 0f;

        // Dialogue first
        if (stage.Dialogue != null && stage.Dialogue.Count > 0)
        {
            ac.DialogueIndex = 0;
            ac.DialogueRunning = true;
            SendDialogueLine(ac);
            return; // wait until dialogues end
        }

        // No dialogue → show choices or progress description immediately
        if (stage.Choices != null && stage.Choices.Count > 0)
        {
            OnChoicesRequired?.Invoke(ac.Data, stage.Choices, ac.Player);
        }
        else
        {
            OnProgressUpdated?.Invoke(ac.Data, 0f, stage.Description, ac.Player);
        }
    }

    private void SendDialogueLine(ActiveEvent ac)
    {
        var dlg = ac.CurrentStage.Dialogue[ac.DialogueIndex];
        ac.DialogueStartTime = DateTime.Now;

        // Evaluate visibility conditions
        if (dlg.VisibilityConditions != null && dlg.VisibilityConditions.Count > 0 &&
            !EvaluateConditions(dlg.VisibilityConditions, ac.Player))
        {
            // skip invisible line
            ac.DialogueIndex++;
            if (ac.DialogueIndex < ac.CurrentStage.Dialogue.Count) SendDialogueLine(ac);
            else
            {
                ac.DialogueRunning = false;
                if (ac.CurrentStage.Choices != null && ac.CurrentStage.Choices.Count > 0)
                    OnChoicesRequired?.Invoke(ac.Data, ac.CurrentStage.Choices, ac.Player);
            }
            return;
        }

        // Use OnProgressUpdated as dialogue conduit (progress = -1 indicates dialogue)
        OnProgressUpdated?.Invoke(ac.Data, -1f, dlg.Text, ac.Player);
    }

    private void AdvanceToNextStage(ActiveEvent ac, string nextStageId)
    {
        var next = ac.Data.Stages.FirstOrDefault(s => s.StageId == nextStageId);
        if (next == null)
        {
            // finish event
            var res = BuildEventResult(ac.Data, ac.Player, true);
            FinishEvent(ac, res);
        }
        else
        {
            ac.CurrentStage = next;
            ac.DialogueRunning = false;
            EnterStage(ac);
        }
    }

    private void AutoFailStage(ActiveEvent ac)
    {
        var res = BuildEventResult(ac.Data, ac.Player, false);
        FinishEvent(ac, res);
    }

    private EventResult BuildEventResult(GameEvent ev, ICharacter player, bool success)
    {
        var res = new EventResult
        {
            Success = success,
            CompletedStageId = ev.Stages.Last().StageId,
            ChoicesMade = CollectChoices(ev),
            AppliedEffects = new Dictionary<string, float>(),
            CompletionTime = DateTime.Now,
        };
        res.NewlyUnlockedContent = EffectSystem != null ? EffectSystem.GetUnlockedContentFromEvent(ev, res) : new List<UnlockedContent>();
        return res;
    }

    private List<string> CollectChoices(GameEvent ev)
    {
        var list = new List<string>();
        if (ev.EventData == null) return list;
        foreach (var kv in ev.EventData)
            if (kv.Key.StartsWith("choice_")) list.Add(kv.Value.ToString());
        return list;
    }

    private void FinishEvent(ActiveEvent ac, EventResult res)
    {
        EffectSystem?.ApplyEventEffects(ac.Data, res, ac.Player);
        OnEventCompleted?.Invoke(ac.Data, res, ac.Player);
        _active.Remove(ac.Data.Id);
    }
    #endregion

    #region ░░ Condition evaluation ░░
    private bool EvaluateConditions(List<EventCondition> conditions, ICharacter player)
    {
        foreach (var c in conditions)
            if (!EvaluateCondition(c, player)) return false; // implicit AND
        return true;
    }

    private bool EvaluateCondition(EventCondition c, ICharacter player)
    {
        switch (c.Type)
        {
            case ConditionType.Time:
                return EvaluateTime(c);
            case ConditionType.Relationship:
                return EvaluateRelationship(c, player);
            case ConditionType.State:
                return EvaluateState(c, player);
            case ConditionType.Location:
                return EvaluateLocation(c, player);
            case ConditionType.Compound:
                return EvaluateCompound(c, player);
            default:
                return true;
        }
    }

    private bool EvaluateCompound(EventCondition cond, ICharacter player)
    {
        if (cond.SubConditions == null || cond.SubConditions.Count == 0) return true;
        bool and = cond.SubConditionOperator == LogicalOperator.AND;
        foreach (var sub in cond.SubConditions)
        {
            bool ok = EvaluateCondition(sub, player);
            if (and && !ok) return false;
            if (!and && ok) return true;
        }
        return and;
    }

    private bool EvaluateTime(EventCondition cond)
    {
        DateTime now = DateTime.Now;
        DateTime expect = (DateTime)cond.ExpectedValue;
        return Compare(now, expect, cond.Operator);
    }

    private bool EvaluateRelationship(EventCondition cond, ICharacter player)
    {
        var rels = player.GetRelationships();
        if (!rels.TryGetValue(cond.TargetId, out float val)) return false;
        float exp = Convert.ToSingle(cond.ExpectedValue);
        return Compare(val, exp, cond.Operator);
    }

    private bool EvaluateState(EventCondition cond, ICharacter player)
    {
        if (cond.TargetId.StartsWith("flag:"))
        {
            bool flag = player.HasFlag(cond.TargetId.Substring(5));
            bool exp = Convert.ToBoolean(cond.ExpectedValue);
            return Compare(flag, exp, cond.Operator);
        }
        var st = player.GetState();
        if (!st.TryGetValue(cond.TargetId, out var obj)) return false;
        if (obj is float f)
            return Compare(f, Convert.ToSingle(cond.ExpectedValue), cond.Operator);
        if (obj is string s)
            return Compare(s, cond.ExpectedValue.ToString(), cond.Operator);
        return obj.Equals(cond.ExpectedValue);
    }

    private bool EvaluateLocation(EventCondition cond, ICharacter player)
    {
        string loc = player.GetCurrentLocation();
        string exp = cond.ExpectedValue.ToString();
        return Compare(loc, exp, cond.Operator);
    }

    // Generic comparers
    private static bool Compare(float a, float b, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equal => Mathf.Abs(a - b) < 0.001f,
            ComparisonOperator.NotEqual => Mathf.Abs(a - b) >= 0.001f,
            ComparisonOperator.GreaterThan => a > b,
            ComparisonOperator.LessThan => a < b,
            ComparisonOperator.GreaterThanOrEqual => a >= b,
            ComparisonOperator.LessThanOrEqual => a <= b,
            _ => false
        };
    }
    private static bool Compare(string a, string b, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equal => a == b,
            ComparisonOperator.NotEqual => a != b,
            ComparisonOperator.Contains => a.Contains(b),
            ComparisonOperator.NotContains => !a.Contains(b),
            _ => false
        };
    }
    private static bool Compare(bool a, bool b, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equal => a == b,
            ComparisonOperator.NotEqual => a != b,
            _ => false
        };
    }
    private static bool Compare(DateTime a, DateTime b, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equal => a == b,
            ComparisonOperator.NotEqual => a != b,
            ComparisonOperator.GreaterThan => a > b,
            ComparisonOperator.LessThan => a < b,
            ComparisonOperator.GreaterThanOrEqual => a >= b,
            ComparisonOperator.LessThanOrEqual => a <= b,
            _ => false
        };
    }
    #endregion

    #region ░░ Availability helpers ░░
    private bool IsEventAvailable(GameEvent ev, ICharacter player)
    {
        if (ev.ExpirationDate.HasValue && DateTime.Now > ev.ExpirationDate) return false;
        if (ev.CooldownPeriod.HasValue && _lastRun.TryGetValue(ev.Id, out var last) && DateTime.Now - last < ev.CooldownPeriod.Value) return false;
        if (!ev.IsRepeatable && player.GetEventHistory().ContainsKey(ev.Id)) return false;
        if (ev.DependentEvents != null)
            foreach (var d in ev.DependentEvents)
                if (!player.GetEventHistory().ContainsKey(d)) return false;
        return CheckEventConditions(ev, player);
    }
    #endregion
}

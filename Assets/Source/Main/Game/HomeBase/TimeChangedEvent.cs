using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocialActivity;

/// <summary>
/// Simple event to notify time changes
/// </summary>
public class TimeChangedEvent
{
    public GameDate PreviousTime { get; set; }
    public GameDate CurrentTime { get; set; }

    public TimeChangedEvent(GameDate previousTime, GameDate currentTime)
    {
        PreviousTime = previousTime;
        CurrentTime = currentTime;
    }
}
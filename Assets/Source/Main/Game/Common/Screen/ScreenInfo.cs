using System;
using UnityEngine;

[Serializable]
public class ScreenInfo
{
    public ScreenManager.ScreenType screenType;
    public BaseScreen prefab;  // This is the prefab for that screen
}

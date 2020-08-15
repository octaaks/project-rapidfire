using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum GameMode
{
    FFA = 0,
    TDM = 1
}

public class GameSettings : MonoBehaviour
{
    public static GameMode gameMode = GameMode.FFA;
    public static bool IsAwayTeam = false;
}

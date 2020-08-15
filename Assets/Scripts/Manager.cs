﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerInfo
{
    public ProfileData profile;
    public int actor;
    public short kills;
    public short deaths;
    public bool awayTeam;

    public PlayerInfo(ProfileData p, int a, short k, short d, bool t)
    {
        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;
        this.awayTeam = t;
    }
}

public enum GameState
{
    Waiting = 0,
    Starting = 1,
    Playing = 2,
    Ending = 3
}

public class Manager : MonoBehaviourPunCallbacks, IOnEventCallback
{

    public int mainmenu = 0;
    public int killcount = 3;
    public int matchLength = 180;
    public bool perpetual = false;

    public string player_prefab_string;
    public GameObject player_prefab;
    public GameObject mapcam;
    public Transform[] spawn_points;

    public List<PlayerInfo> playerInfo = new List<PlayerInfo>();
    public int myind;

    private bool playerAdded;

    private TextMeshProUGUI ui_mykills;
    private TextMeshProUGUI ui_mydeaths;
    private TextMeshProUGUI ui_timer;

    private Transform ui_leaderboard;
    private Transform ui_endgame;

    private int currentMatchTime;
    private Coroutine timerCoroutine;

    private GameState state = GameState.Waiting;

    public enum EventCodes : byte
    {
        NewPlayer,
        UpdatePlayers,
        ChangeStat,
        NewMatch,
        RefreshTimer
    }

    private void Start()
    {
        mapcam.SetActive(false);

        ValidateConnection();
        InitializeUI();
        InitializeTimer();
        NewPlayer_S(Launcher.myProfile);

        if (PhotonNetwork.IsMasterClient)
        {
            playerAdded = true;
            Spawn();
        }
    }

    private void InitializeUI()
    {
        ui_mykills = GameObject.Find("HUD/KD/Kills").GetComponent<TextMeshProUGUI>();
        ui_mydeaths = GameObject.Find("HUD/KD/Deaths").GetComponent<TextMeshProUGUI>();
        ui_timer = GameObject.Find("HUD/Timer/Text (TMP)").GetComponent<TextMeshProUGUI>();
        ui_leaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
        ui_endgame = GameObject.Find("Canvas").transform.Find("EndGame").transform;

        RefreshMyStats();
    }

    private void Update()
    {

        if (state == GameState.Ending)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (ui_leaderboard.gameObject.activeSelf)
            {
                ui_leaderboard.gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("Accessing Leaderboard FUnction");
                Leaderboard(ui_leaderboard); ///////////
            }
        }
    }

    private void Leaderboard(Transform p_lb)
    {
        //specify leaderboard
        if (GameSettings.gameMode == GameMode.FFA) p_lb = p_lb.Find("FFA");
        if (GameSettings.gameMode == GameMode.TDM) p_lb = p_lb.Find("TDM");

        // clean up
        for (int i = 2; i < p_lb.childCount; i++)
        {
            Destroy(p_lb.GetChild(i).gameObject);
        }

        // set details
        p_lb.Find("Header/Mode").GetComponent<TextMeshProUGUI>().text = System.Enum.GetName(typeof(GameMode),GameSettings.gameMode);
        p_lb.Find("Header/Map").GetComponent<TextMeshProUGUI>().text = SceneManager.GetActiveScene().name;

        // set scores
        if (GameSettings.gameMode == GameMode.TDM)
        {
            p_lb.Find("Header/Score/Home").GetComponent<TextMeshProUGUI>().text = "0";
            p_lb.Find("Header/Score/Away").GetComponent<TextMeshProUGUI>().text = "0";
        }

        // cache prefab
        GameObject playercard = p_lb.GetChild(1).gameObject;
        playercard.SetActive(false);

        // sort
        List<PlayerInfo> sorted = SortPlayers(playerInfo);

        // display
        bool t_alternateColors = false;

        foreach (PlayerInfo a in sorted)
        {
            GameObject newcard = Instantiate(playercard, p_lb) as GameObject;

            if (GameSettings.gameMode == GameMode.TDM)
            {
                newcard.transform.Find("Home").gameObject.SetActive(!a.awayTeam);
                newcard.transform.Find("Away").gameObject.SetActive(a.awayTeam);
            }

            if (t_alternateColors) newcard.GetComponent<Image>().color = new Color32(0, 0, 0, 180);
            t_alternateColors = !t_alternateColors;

            newcard.transform.Find("Level").GetComponent<TextMeshProUGUI>().text = a.profile.level.ToString("00");
            newcard.transform.Find("Username").GetComponent<TextMeshProUGUI>().text = a.profile.username;
            newcard.transform.Find("ScoreValue").GetComponent<TextMeshProUGUI>().text = (a.kills * 100).ToString();
            newcard.transform.Find("KillValue").GetComponent<TextMeshProUGUI>().text = a.kills.ToString();
            newcard.transform.Find("DeathValue").GetComponent<TextMeshProUGUI>().text = a.deaths.ToString();

            newcard.SetActive(true);
        }

        // activate
        p_lb.gameObject.SetActive(true);
        p_lb.parent.gameObject.SetActive(true);
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> p_info)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        if(GameSettings.gameMode == GameMode.FFA)
        {
            while(sorted.Count < p_info.Count)
            {
                //set defaults
                short highest = -1;
                PlayerInfo selection = p_info[0];

                //grab next player 
                foreach(PlayerInfo a in p_info)
                {
                    if (sorted.Contains(a)) continue;
                    if(a.kills > highest)
                    {
                        highest = a.kills;
                    }
                }

                //add player
                sorted.Add(selection);
;            }
        }

        if (GameSettings.gameMode == GameMode.TDM)
        {

            List<PlayerInfo> homeSorted = new List<PlayerInfo>();
            List<PlayerInfo> awaySorted = new List<PlayerInfo>();

            int homeSize = 0;
            int awaySize = 0;

            foreach(PlayerInfo p in p_info)
            {
                if (p.awayTeam) awaySize++;
                else homeSize++;
            }

            while (homeSorted.Count < homeSize)
            {
                //set defaults
                short highest = -1;
                PlayerInfo selection = p_info[0];

                //grab next player 
                foreach (PlayerInfo a in p_info)
                {
                    if (a.awayTeam) continue;
                    if (homeSorted.Contains(a)) continue;
                    if (a.kills > highest)
                    {
                        selection = a;
                        highest = a.kills;
                    }
                }
                //add player
                homeSorted.Add(selection);
            }


            while (awaySorted.Count < homeSize)
            {
                //set defaults
                short highest = -1;
                PlayerInfo selection = p_info[0];

                //grab next player 
                foreach (PlayerInfo a in p_info)
                {
                    if (!a.awayTeam) continue;
                    if (awaySorted.Contains(a)) continue;
                    if (a.kills > highest)
                    {
                        selection = a;
                        highest = a.kills;
                    }
                }
                //add player
                awaySorted.Add(selection);
            }
            sorted.AddRange(homeSorted);
            sorted.AddRange(awaySorted);
        }
        return sorted;
    }


    private void RefreshMyStats()
    {
        if (playerInfo.Count > myind)
        {
            ui_mykills.text = $"{playerInfo[myind].kills} kills";
            ui_mydeaths.text = $"{playerInfo[myind].deaths} deaths";
        }
        else
        {
            ui_mykills.text = "0 kills";
            ui_mydeaths.text = "0 deaths";
        }
    }

    public void Spawn()
    {
        Transform t_spawn = spawn_points[Random.Range(0, spawn_points.Length)];

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Instantiate(player_prefab_string, t_spawn.position, t_spawn.rotation);
        }
        else
        {
            Debug.Log("Working");
            GameObject newPlayer = Instantiate(player_prefab, t_spawn.position, t_spawn.rotation) as GameObject;
        }
    }

    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(mainmenu);
    }

    private void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
        }
    }

    private void ScoreCheck()
    {
        bool detectwin = false;

        foreach (PlayerInfo a in playerInfo)
        {
            if (a.kills >= killcount)
            {
                detectwin = true;
                break;
            }
        }

        if (detectwin)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                UpdatePlayers_S((int)GameState.Ending, playerInfo);
            }
        }
    }

    private void InitializeTimer()
    {
        currentMatchTime = matchLength;
        RefreshTimerUI();

        if (PhotonNetwork.IsMasterClient)
        {
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private void RefreshTimerUI()
    {
        string minutes = (currentMatchTime / 60).ToString("00");
        string seconds = (currentMatchTime % 60).ToString("00");
        ui_timer.text = $"{minutes}:{seconds}";
    }

    private void EndGame()
    {
        state = GameState.Ending;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        currentMatchTime = 0;
        RefreshTimerUI();

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
            if (!perpetual)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
        }

        mapcam.SetActive(true);

        ui_endgame.gameObject.SetActive(true);
        ui_leaderboard.gameObject.SetActive(false);

        Debug.Log("Accessing Leaderboard FUnction");
        Leaderboard(ui_endgame.Find("Leaderboard")); //////////////

        StartCoroutine(End(6f));

    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;

        EventCodes e = (EventCodes)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;

        switch (e)
        {
            case EventCodes.NewPlayer:
                NewPlayer_R(o);
                break;
            case EventCodes.UpdatePlayers:
                UpdatePlayers_R(o);
                break;
            case EventCodes.ChangeStat:
                ChangeStat_R(o);
                break;
            case EventCodes.RefreshTimer:
                RefreshTimer_R(o);
                break;
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(mainmenu);
    }

    private bool CalculateTeam()
    {
        return PhotonNetwork.CurrentRoom.PlayerCount %2==0;
    }

    public void NewPlayer_S(ProfileData p)
    {
        object[] package = new object[7];
        package[0] = p.username;
        package[1] = p.level;
        package[2] = p.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short)0;
        package[5] = (short)0;
        package[6] = CalculateTeam();

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
            );
    }

    public void NewPlayer_R(object[] data)
    {
        PlayerInfo p = new PlayerInfo(
            new ProfileData(
                (string)data[0],
                (int)data[1],
                (int)data[2]
                ),
            (int)data[3],
            (short)data[4],
            (short)data[5],
            (bool)data[6]
            );

        playerInfo.Add(p);

        //resync our local player information with the new player
        foreach(GameObject gameObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            gameObject.GetComponent<Player>().TrySync();
        }

        UpdatePlayers_S((int)state, playerInfo);
    }

    public void UpdatePlayers_S(int state, List<PlayerInfo> info)
    {
        object[] package = new object[info.Count + 1];
        package[0] = state;

        for (int i = 0; i < info.Count; i++)
        {
            object[] piece = new object[7];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.level;
            piece[2] = info[i].profile.xp;
            piece[3] = info[i].actor;
            piece[4] = info[i].kills;
            piece[5] = info[i].deaths;
            piece[6] = info[i].awayTeam;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdatePlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void UpdatePlayers_R(object[] data)
    {
        state = (GameState)data[0];

        //check if there is a new player
        if(playerInfo.Count < data.Length - 1)
        {
            foreach(GameObject gameObject in GameObject.FindGameObjectsWithTag("Player"))
            {
                gameObject.GetComponent<Player>().TrySync();
            }
        }

        playerInfo = new List<PlayerInfo>();

        for (int i = 1; i < data.Length; i++)
        {
            object[] extract = (object[])data[i];

            PlayerInfo p = new PlayerInfo(
                new ProfileData(
                    (string)extract[0],
                    (int)extract[1],
                    (int)extract[2]
                ),
                (int)extract[3],
                (short)extract[4],
                (short)extract[5],
                (bool)extract[6]
                );
            playerInfo.Add(p);
            if (PhotonNetwork.LocalPlayer.ActorNumber == p.actor)
            {
                myind = i - 1;

                if (!playerAdded)
                {
                    playerAdded = true;
                    GameSettings.IsAwayTeam = p.awayTeam;
                    Spawn();
                }
            }
        }

        StateCheck();
    }

    public void ChangeStat_S(int actor, byte stat, byte amt)
    {
        object[] package = new object[] { actor, stat, amt };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ChangeStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void ChangeStat_R(object[] data)
    {
        int actor = (int)data[0];
        byte stat = (byte)data[1];
        byte amt = (byte)data[2];

        for (int i = 0; i < playerInfo.Count; i++)
        {
            if (playerInfo[i].actor == actor)
            {
                switch (stat)
                {
                    case 0: //kills
                        playerInfo[i].kills += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username} : kills = {playerInfo[i].kills}");
                        break;
                    case 1: //deaths
                        playerInfo[i].deaths += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username} : deaths = {playerInfo[i].deaths}");
                        break;
                }

                if (i == myind) RefreshMyStats();
                if (ui_leaderboard.gameObject.activeSelf)
                {
                    Leaderboard(ui_leaderboard); ////////////
                    Debug.Log("Accessing Leaderboard FUnction");
                }

                break;
            }
        }
        ScoreCheck();
    }

    public void NewMatch_S()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void NewMatch_R()
    {
        state = GameState.Waiting;
        mapcam.SetActive(false);
        ui_endgame.gameObject.SetActive(false);

        foreach(PlayerInfo p in playerInfo){
            p.kills = 0;
            p.deaths = 0;
        }

        RefreshMyStats();
        InitializeTimer();
        Spawn();
    }

    public void RefreshTimer_S()
    {
        object[] package = new object[] { currentMatchTime };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.RefreshTimer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void RefreshTimer_R(object[] data)
    {
        currentMatchTime = (int)data[0];
        RefreshTimerUI();
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(1f);

        currentMatchTime -= 1;

        if(currentMatchTime <= 1)
        {
            timerCoroutine = null;
            UpdatePlayers_S((int)GameState.Ending, playerInfo);
        }
        else
        {
            RefreshTimer_S();
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private IEnumerator End(float p_wait)
    {
        yield return new WaitForSeconds(p_wait);
        
        if (perpetual)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NewMatch_S();
            }
        }
        else
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
    }
}

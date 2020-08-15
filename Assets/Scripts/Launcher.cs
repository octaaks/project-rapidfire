using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

[System.Serializable]
public class ProfileData
{
    public string username;
    public int level;
    public int xp;

    public ProfileData()
    {
        this.username = "";
        this.level = 1;
        this.xp = 0;
    }

    public ProfileData(string u, int l, int x)
    {
        this.username = u;
        this.level = l;
        this. xp = x;
    }

    //object[] ConvertToObjectArr()
    //{
    //    object[] ret = new object[3];

    //    return ret;
    //}
}

[System.Serializable]

public class MapData
{
    public string name;
    public int scene;
}

public class Launcher : MonoBehaviourPunCallbacks
{
    public TMP_InputField usernameField;
    public TMP_InputField roomnameField;
    public TextMeshProUGUI mapValue;
    public TextMeshProUGUI modeValue;
    public Slider maxPlayerSlider;
    public TextMeshProUGUI maxPlayerValue;
    public static ProfileData myProfile = new ProfileData();

    public GameObject tabMain;
    public GameObject tabRooms;
    public GameObject tabCreate;
    public GameObject tabSettings;
    public GameObject buttonRoom;
    public GameObject splashScreen;

    public MapData[] maps;
    private int currentmap = 0;

    private List<RoomInfo> roomList;

    public void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        myProfile = Data.LoadProfile();
        if (!string.IsNullOrEmpty(myProfile.username))
        {
            usernameField.text = myProfile.username;
        }
        Invoke("Connect",3f);
    }

    public override void OnConnectedToMaster()
    {
        splashScreen.SetActive(false);
        Debug.Log("Connected TO Master!!!");

        PhotonNetwork.JoinLobby();
        base.OnConnectedToMaster();
    }

    public override void OnJoinedRoom()
    {
        StartGame();

        base.OnJoinedRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Create();
        base.OnJoinRandomFailed(returnCode, message);
    }

    public void Connect()
    {
        Debug.Log("Trying to connect...");
        PhotonNetwork.GameVersion = "0.0.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Join()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void Create()
    {
        RoomOptions options = new RoomOptions();

        options.MaxPlayers = (byte)maxPlayerSlider.value;
        options.CustomRoomPropertiesForLobby = new string[] { "map", "mode" };

        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
        properties.Add("map", currentmap);
        properties.Add("mode", (int)GameSettings.gameMode);

        options.CustomRoomProperties = properties;
        PhotonNetwork.CreateRoom(roomnameField.text, options);
    }

    public void ChangeMap()
    {
        currentmap++;
        if (currentmap >= maps.Length) currentmap = 0;
        mapValue.text = "MAP: " + maps[currentmap].name.ToUpper();
    }

    public void ChangeMode()
    {
        int newMode = (int)GameSettings.gameMode + 1;
        if (newMode >= System.Enum.GetValues(typeof(GameMode)).Length) newMode = 0;
        GameSettings.gameMode = (GameMode)newMode;
        modeValue.text = "MODE: " + System.Enum.GetName(typeof(GameMode), newMode);
    }

    public void ChangeMaxPlayerSlider(float t_value)
    {
        maxPlayerValue.text = Mathf.RoundToInt(t_value).ToString();
    }

    public void TabCloseAll()
    {
        tabMain.SetActive(false);
        tabRooms.SetActive(false);
        tabCreate.SetActive(false);
        tabSettings.SetActive(false);
    }

    public void TabOpenMain()
    {
        TabCloseAll();
        tabMain.SetActive(true);
    }

    public void TabOpenRooms()
    {
        TabCloseAll();
        tabRooms.SetActive(true);
    }

    public void TabOpenSettings()
    {
        TabCloseAll();
        tabSettings.SetActive(true);
    }

    public void TabOpenCreate()
    {
        TabCloseAll();
        tabCreate.SetActive(true);

        roomnameField.text = "";

        currentmap = 0;
        mapValue.text = "MAP: " + maps[currentmap].name.ToUpper();

        GameSettings.gameMode = (GameMode)0;
        modeValue.text = "Mode " + System.Enum.GetName(typeof(GameMode), (GameMode)0);

        maxPlayerSlider.value = maxPlayerSlider.maxValue;
        maxPlayerValue.text = Mathf.RoundToInt(maxPlayerSlider.value).ToString();
    }

    private void ClearRoomList()
    {
        Transform content = tabRooms.transform.Find("Scroll View/Viewport/Content");
        foreach (Transform a in content) Destroy(a.gameObject);
    }

    public override void OnRoomListUpdate(List<RoomInfo> p_list)
    {
        roomList = p_list;

        ClearRoomList();

        //Debug.Log("Loaded Rooms @ " + Time.deltaTime);
        Transform content = tabRooms.transform.Find("Scroll View/Viewport/Content");

        foreach (RoomInfo a in roomList)
        {
            GameObject newRoomButton = Instantiate(buttonRoom, content) as GameObject;
            newRoomButton.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = a.Name;
            newRoomButton.transform.Find("Players").GetComponent<TextMeshProUGUI>().text = a.PlayerCount + " / " + a.MaxPlayers;

            if (a.CustomProperties.ContainsKey("map"))
            {
                newRoomButton.transform.Find("Map/Name").GetComponent<TextMeshProUGUI>().text = maps[(int)a.CustomProperties["map"]].name;
            }
            else
            {
                newRoomButton.transform.Find("Map/Name").GetComponent<TextMeshProUGUI>().text = "__________";
            }

            newRoomButton.GetComponent<Button>().onClick.AddListener(delegate { JoinRoom(newRoomButton.transform); });

        }
        base.OnRoomListUpdate(roomList);
    }
    public void JoinRoom(Transform p_button)
    {
        //Debug.Log("Joining Room @" + Time.time);
        string t_roomName = p_button.transform.Find("Name").GetComponent<TextMeshProUGUI>().text;
        
        VerifyUsername();

        RoomInfo roomInfo = null;
        Transform buttonParent = p_button.parent;

        for (int i = 0; i < buttonParent.childCount; i++)
        {
            if (buttonParent.GetChild(i).Equals(p_button))
            {
                roomInfo = roomList[i];
                break;
            }
        }
        if(roomInfo != null)
        {
            LoadGameSettings(roomInfo);
            PhotonNetwork.JoinRoom(t_roomName);
        }
    }

    public void LoadGameSettings(RoomInfo roomInfo)
    {
        GameSettings.gameMode = (GameMode)roomInfo.CustomProperties["mode"];
        Debug.Log(System.Enum.GetName(typeof(GameMode), GameSettings.gameMode));
    }

    public void StartGame()
    {
        VerifyUsername();

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            Data.SaveProfile(myProfile);
            PhotonNetwork.LoadLevel(maps[currentmap].scene);
        }
    }

    private void VerifyUsername()
    {
        if (string.IsNullOrEmpty(usernameField.text))
        {
            myProfile.username = "GUEST_" + Random.Range(100, 1000);
        }
        else
        {
            myProfile.username = usernameField.text;
        }
    }
}

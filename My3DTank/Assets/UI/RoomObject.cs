using System;
using System.Collections;
using System.Collections.Generic;
public class RoomObject
{
    private string roomName;
    private string hostName;
    private int existPerNum;
    private string fraction;
    private List<Player> playerList;
    public RoomObject(string roomName)
    {
        this.roomName = roomName;
        //房主的名字在点击 list1 中的entry 发送 AskForSpeRoom时 服务器广播房间内玩家 传过来的第一个就是房主
        this.hostName = "";
        this.fraction = "1/15";
        this.playerList = new List<Player>();
        this.existPerNum = 1;
    }
    public string RoomName
    {
        get { return roomName; }
        set { roomName = value; }
    }
    public string HostName
    {
        get { return hostName; }
        set { hostName = value; }
    }
    public int ExistPerNum
    {
        get { return existPerNum; }
        set { existPerNum = value; }
    }
    public string Fraction
    {
        get { return fraction; }
        set { fraction = value; }
    }
    public List<Player> PlayerList
    {
        get { return playerList; }
        set { playerList = value; }
    }
    public static bool DetectRoomName(string name, List<RoomObject> roomList)
    {
        foreach (RoomObject item in roomList)
        {
            if (name.Equals(item.roomName))
                return true;
        }
        return false;
    }
    public void GenerateFraction()
    {
        fraction = "" + existPerNum + "/15";
    }
}
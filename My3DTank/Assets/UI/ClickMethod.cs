using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;


public class ClickMethod : MonoBehaviour
{
    public Button createButton = null;

    private List<GameObject> items = new List<GameObject>();
    private List<RoomObject> roomList = new List<RoomObject>(); //房间对象的表
    public GameObject content1GObject;  //房间列表content 的object
    public GameObject content3GObject;  //房间内玩家列表content 的object
    public GameObject inputField;   //输入框（输入房间名）
    public Vector2 content1Size;    //房间列表content的原始高度
    public Vector2 content3Size;    //房间内玩家列表content的原始高度

    //房间列表
    public GameObject itemGObject1; //列表项的 奇数项
    public GameObject itemGObject2; //列表项的 偶数项
    public float itemHeight1;
    public Vector3 itemLocalPos1;

    //房间内玩家列表
    public GameObject itemGObject3; //列表项的 奇数项
    public GameObject itemGObject4; //列表项的 偶数项
    public float itemHeight3;
    public Vector3 itemLocalPos3;

    //当前的房间
    private RoomObject currentRoom;


    // Use this for initialization
    void Start()
    {
        //这个判断仅仅只是为了方便测试 测试内容是直接挂到Button上
        if (createButton == null)
            createButton = GameObject.Find("CreateButton").GetComponent<Button>();
        content1GObject = createButton.transform.parent.Find("ScrollRoomListView").Find("Viewport").Find("Content").gameObject;
        content3GObject = createButton.transform.parent.Find("PlayerInRoomList").Find("Viewport").Find("Content").gameObject;
        inputField = createButton.transform.parent.Find("InputField").gameObject;
        content1Size = content1GObject.GetComponent<RectTransform>().sizeDelta;
        content3Size = content3GObject.GetComponent<RectTransform>().sizeDelta;

        //加载  房间列表 content 相关参数
        itemGObject1 = Resources.Load("Text1") as GameObject;
        itemGObject2 = Resources.Load("Text2") as GameObject;
        itemHeight1 = itemGObject1.GetComponent<RectTransform>().rect.height;
        itemLocalPos1 = itemGObject1.transform.localPosition;

        //加载  房间内玩家列表 content 相关参数
        itemGObject3 = Resources.Load("Text3") as GameObject;
        itemGObject4 = Resources.Load("Text4") as GameObject;
        itemHeight3 = itemGObject3.GetComponent<RectTransform>().rect.height;
        itemLocalPos3 = itemGObject3.transform.localPosition;
    }
    void Start(Button createButton)
    {
        this.createButton = createButton;
        Start();
    }
    
    #region createbutton 上的方法 以及点击 房间列表中的 item 的点击方法
    //挂到 Button 上的方法
    public void CreateRoom()
    {
        string roomName = inputField.GetComponent<InputField>().text;
        //增强鲁棒性
        if (roomName.Equals(""))
        {                               //没有输入
            UnityEditor.EditorUtility.DisplayDialog("警告", "房间名不能为空", "确认", "取消");
            return;
        }
        else if (RoomObject.DetectRoomName(roomName, roomList))
        {   //有重复的名字
            UnityEditor.EditorUtility.DisplayDialog("警告", "该房间名已存在", "确认", "取消");
            return;
        }
        //上方判断通过了后 就可以加入 roomList 啦
        RoomObject thisRoom = new RoomObject(roomName);
        
        thisRoom.PlayerList.Add(new Player("hahaha"));
        currentRoom = thisRoom;         //这个房间就是创建者所在的房间啦
        roomList.Add(thisRoom);         //在所有的房间列表中 加入这个房间
        // TODO: 通过协议将 新建房间  信息传给服务器
        

        // 解释一下 (items.Count+1)/2 
        // 因为我的一行有两个text 所以 这个是求行数最简单的办法
        GameObject a1 = Instantiate(itemGObject1) as GameObject;
        GameObject a2 = Instantiate(itemGObject2) as GameObject;
        a1.transform.SetParent(content1GObject.transform);
        a2.transform.SetParent(content1GObject.transform);
        a1.transform.localPosition = new Vector3(itemLocalPos1.x, itemLocalPos1.y - (items.Count + 1) / 2 * itemHeight1, 0);
        a2.transform.localPosition = new Vector3(itemLocalPos1.x, itemLocalPos1.y - (items.Count + 1) / 2 * itemHeight1, 0);
        a1.GetComponent<Text>().text = roomName;
        a2.GetComponent<Text>().text = thisRoom.Fraction;
        items.Add(a1);
        items.Add(a2);
        if (content1Size.y <= ((items.Count + 1) / 2) * itemHeight1)
        {
            content1GObject.GetComponent<RectTransform>().sizeDelta = new Vector2(content1Size.x, ((items.Count + 1) / 2) * itemHeight1);
        }

        //来给 新加入的两个 Text 添加监听吧 同时将它们自己传入
        a1.GetComponent<Button>().onClick.AddListener(delegate ()
        {
            OnTextA1CLick(this.gameObject);
        });
        a2.GetComponent<Button>().onClick.AddListener(delegate ()
        {
            OnTextA2CLick(this.gameObject);
        });

        //因为这个方法里 有对于新房间的创建 所以最后需要自动 在房间内列表 content 里面显示所有的玩家
        OnTextA1CLick(a1);
    }
    void OnTextA1CLick(GameObject objA1)
    {
        //假如点击的是第三个 那么传入2
        AddAllItemToInRoomList((items.FindIndex(x => x == objA1) + 1) / 2);
    }
    void OnTextA2CLick(GameObject objA2)
    {
        //假如点击是第四个 那么传入2
        AddAllItemToInRoomList((items.FindIndex(x => x == objA2)) / 2);
    }
    void AddAllItemToInRoomList(int index)
    {
        RoomObject room = roomList[index];
        //TODO: 从服务器获取这个名字的房间
        GameObject a3 = Instantiate(itemGObject3) as GameObject;
        GameObject a4 = Instantiate(itemGObject4) as GameObject;
        a3.transform.SetParent(content3GObject.transform);
        a4.transform.SetParent(content3GObject.transform);
        //在 list 中的第一个的就是房主  所以跳过第一个
        AddEachPlayerItem(room.PlayerList[0].Name, a3, a4, "host");
        for (int i = 1; i < room.PlayerList.Count; i++)
            AddEachPlayerItem(room.PlayerList[i].Name, a3, a4, "");
    }
    //join
    public void ClickJoinButton()
    {
        RoomObject room = new RoomObject("aaa");
        room.PlayerList.Add(new Player("aaaa1"));
        room.PlayerList.Add(new Player("bbbb1"));
        room.PlayerList.Add(new Player("Cccc3"));

        
        //在 list 中的第一个的就是房主  所以跳过第一个
        AddEachPlayerItem(room.PlayerList[0].Name, "host");
        for (int i = 1; i < room.PlayerList.Count; i++)
            AddEachPlayerItem(room.PlayerList[i].Name, "");
    }
    void AddEachPlayerItem(string playerIP, string state)
    {
        GameObject a3 = Instantiate(itemGObject3) as GameObject;
        GameObject a4 = Instantiate(itemGObject4) as GameObject;
        a3.transform.SetParent(content3GObject.transform);
        a4.transform.SetParent(content3GObject.transform);
        a3.transform.localPosition = new Vector3(itemLocalPos3.x, itemLocalPos3.y - ((items.Count + 1) / 2) * itemHeight3, 0);
        a4.transform.localPosition = new Vector3(itemLocalPos3.x, itemLocalPos3.y - ((items.Count + 1) / 2) * itemHeight3, 0);
        a3.GetComponent<Text>().text = playerIP;
        a4.GetComponent<Text>().text = state;
        items.Add(a3);
        items.Add(a4);
        if (content3Size.y <= ((items.Count + 1) / 2) * itemHeight3)
        {
            content3GObject.GetComponent<RectTransform>().sizeDelta = new Vector2(content3Size.x, ((items.Count + 1) / 2) * itemHeight3);
        }
    }
    /**
	 *   配合上面的遍历   将遍历中的每一个 player 都创建两个 Text 然后插入房间内玩家列表的 content
	 */
    void AddEachPlayerItem(string playerIP, GameObject a3, GameObject a4, string state)
    {
        a3.transform.localPosition = new Vector3(itemLocalPos3.x, itemLocalPos3.y - ((items.Count + 1) / 2) * itemHeight3, 0);
        a4.transform.localPosition = new Vector3(itemLocalPos3.x, itemLocalPos3.y - ((items.Count + 1) / 2) * itemHeight3, 0);
        a3.GetComponent<Text>().text = playerIP;
        a4.GetComponent<Text>().text = state;
        items.Add(a3);
        items.Add(a4);
        if (content3Size.y <= ((items.Count + 1) / 2) * itemHeight3)
        {
            content3GObject.GetComponent<RectTransform>().sizeDelta = new Vector2(content3Size.x, ((items.Count + 1) / 2) * itemHeight3);
        }
    }

    /**
	 *	得到本机的ip
	 */
    public string GetIPAddress()
    {
        string hostname = Dns.GetHostName();
        IPHostEntry ipadrlist = Dns.GetHostEntry(hostname);
        IPAddress localaddr = ipadrlist.AddressList[0];
        return localaddr.ToString();
    }
    #endregion
    #region leavebutton 上的方法
    void Leave()
    {
        if (GetIPAddress().Equals(currentRoom.PlayerList[0]))
        {   //我要离开自己创建的房间啦

        }
    }
    #endregion
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FirstPanelScript : PanelBase
{
    private Button createButton;
    private Button joinButton;
    private Button beginButton;
    private Button leaveButton;


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

    public RoomObject currentRoom; //当前的房间
    public RoomObject selectedRoom;//左侧单击后选择的房间

    // 显示选择的房间的文本
    private GameObject room_tag;


    #region 生命周期
    public override void Init(params object[] args)
    {
        base.Init(args);
        skinPath = "FirstPanel";
        layer = PanelLayer.FirstPanel;
    }

    public override void OnShowing()
    {
        base.OnShowing();
        Transform skinTrans = skin.transform;
        createButton = skinTrans.Find("CreateButton").GetComponent<Button>();
        joinButton = skinTrans.Find("JoinButton").GetComponent<Button>();
        beginButton = skinTrans.Find("BeginButton").GetComponent<Button>();
        leaveButton = skinTrans.Find("LeaveButton").GetComponent<Button>();

        content1GObject = createButton.transform.parent.Find("ScrollRoomListView").Find("Viewport").Find("Content").gameObject;
        content3GObject = createButton.transform.parent.Find("PlayerInRoomList").Find("Viewport").Find("Content").gameObject;
        inputField = createButton.transform.parent.Find("InputField").gameObject;
        room_tag = createButton.transform.parent.Find("Room_Tag").gameObject;
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

        createButton.onClick.AddListener(OnCreateRoomClick);
        joinButton.onClick.AddListener(OnJoinRoomClick);
        beginButton.onClick.AddListener(OnBeginGameClick);
        leaveButton.onClick.AddListener(OnLeaveRoomClick);

        NetMgr.srvConn.msgDist.AddListener("CreateRoom", CreateRoom);
        NetMgr.srvConn.msgDist.AddListener("LeaveRoom", LeaveRoom);
        NetMgr.srvConn.msgDist.AddListener("JoinRoom", JoinRoom);
        NetMgr.srvConn.msgDist.AddListener("StartFight", StartFight);
        //请求所有的房间
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("AskForRooms");
        Debug.Log("发送 " + protocol.GetDesc());
        NetMgr.srvConn.Send(protocol, OnAskForRooms);
    }
    public void OnAskForRooms(ProtocolBase protocol)
    {
        ProtocolBytes proto = (ProtocolBytes)protocol;
        int start = 0;
        /*string protoName = */proto.GetString(start, ref start);

        string roomName;
        int peopleNum;
        RoomObject room;
        roomList.Clear();   //习惯性清空roomList
        while ((roomName = proto.GetString(start, ref start)) != "")
        {
            peopleNum = proto.GetInt(start, ref start);
            //将这个房间加入 总的房间列表 这就是所有房间的初始化过程 但是注意，里面的玩家列表是空的，需要点击后，才能从后端获取
            room = new RoomObject(roomName);
            room.ExistPerNum = peopleNum;
            room.GenerateFraction();    //计算其中的fraction
            roomList.Add(room);         //将其加入 roomList
        }
        if (roomList != null && roomList.Count > 0)
            //渲染 list1 ，呈现所有的房间
            RenderListOne();
        else
            return;
       

    }

    #region OnCreateRoomClick() 以及 列表1 中 entry 的点击事件
    public void OnCreateRoomClick()
    {
        string roomName = inputField.GetComponent<InputField>().text;
        //增强鲁棒性   前端进行判断 真省心
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
        thisRoom.PlayerList.Add(new Player(Root.this_player.Name));  //加入房主

        currentRoom = thisRoom;         //这个房间就是创建者所在的房间啦
        //roomList.Add(thisRoom);//在所有的房间列表中 加入这个房间//在广播回调中加入
        //通过协议将 新建房间  信息传给服务器
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("CreateRoom");
        protocol.AddString(roomName);
        Debug.Log("发送 " + protocol.GetDesc());
        NetMgr.srvConn.Send(protocol);//广播 不需要注册回调函数，但是服务端发过来的数据会分发到那个回调函数上
        //在广播的回调中进行 列表的重新渲染
    }
    //创建房间的广播回调函数
    public void CreateRoom(ProtocolBase protocol)
    {
        Debug.Log("来啊，我在这里");
        ProtocolBytes proto = (ProtocolBytes)protocol;
        int start = 0;
        /*string protoName = */
        proto.GetString(start, ref start);

        string room_name = proto.GetString(start, ref start);
        int peopleNum = proto.GetInt(start, ref start);

        //将房间新房间加入 roomList 然后重新渲染 列表一
        RoomObject room = new RoomObject(room_name)//简化的new
        {
            ExistPerNum = peopleNum
        };
        room.GenerateFraction();
        roomList.Add(room);
        //渲染 列表一 所有房间列表
        RenderListOne();
    }
    //渲染列表
    void RenderListOne()
    {
        //先清空 content
        createButton.transform.parent.Find("ScrollRoomListView").GetComponent<ScrollRect>().content.DetachChildren();
        items.Clear();  //先将存储所有 GameObject 的items 完全清空 然后准备重新装入
        //重新渲染
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomObject item = roomList[i];
            AddItemToScrollRoomList(item.RoomName, item.Fraction);
        }
    }
    //渲染列表一
    void AddItemToScrollRoomList(string roomName, string fraction)
    {   
        // 解释一下 (items.Count+1)/2 
        // 因为我的一行有两个text 所以 这个是求行数最简单的办法
        GameObject a1 = Instantiate(itemGObject1) as GameObject;
        GameObject a2 = Instantiate(itemGObject2) as GameObject;
        a1.transform.SetParent(content1GObject.transform);
        a2.transform.SetParent(content1GObject.transform);
        a1.transform.localPosition = new Vector3(itemLocalPos1.x, itemLocalPos1.y - (items.Count + 1) / 2 * itemHeight1, 0);
        a2.transform.localPosition = new Vector3(itemLocalPos1.x, itemLocalPos1.y - (items.Count + 1) / 2 * itemHeight1, 0);
        a1.GetComponent<Text>().text = roomName;
        a2.GetComponent<Text>().text = fraction;
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
        //先清空 content
        createButton.transform.parent.Find("PlayerInRoomList").GetComponent<ScrollRect>().content.DetachChildren();

        selectedRoom = roomList[index];
        //将显示当前房间名的 Text 设置为 所点击的item的房间名
        room_tag.GetComponent<Text>().text = selectedRoom.RoomName;
        //从服务器获取这个名字的房间 的人员列表
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("AskForSpeRoom");
        protocol.AddString(selectedRoom.RoomName);
        Debug.Log("发送 " + protocol.GetDesc());
        NetMgr.srvConn.Send(protocol, OnAskForSpeRoom);
    }
    public void OnAskForSpeRoom(ProtocolBase protocol)
    {
        ProtocolBytes proto = (ProtocolBytes)protocol;
        int start = 0;
        /*string protoName = */proto.GetString(start, ref start);

        string peopleName;
        //这也是每一个房间 里面的 玩家列表的初始化过程 在这之前 里面是什么也没有的
        selectedRoom.PlayerList.Clear();    //如果上一次试图获取了，先清除，然后重新对 list2进行渲染
        while ((peopleName = proto.GetString(start, ref start)) != "")
        {
            selectedRoom.PlayerList.Add(new Player(peopleName));
        }
        //判断是否为空
        if(selectedRoom.PlayerList != null && selectedRoom.PlayerList.Count > 0)
        {
            items.Clear();
            //在 list 中的第一个的就是房主  所以跳过第一个
            AddEachPlayerItem(selectedRoom.PlayerList[0].Name, "host");
            for (int i = 1; i < selectedRoom.PlayerList.Count; i++)
                AddEachPlayerItem(selectedRoom.PlayerList[i].Name, "");
        }else
            return;
        
    }
    /**
	 *   配合上面的遍历   将遍历中的每一个 player 都创建两个 Text 然后插入房间内玩家列表的 content
	 */
    void AddEachPlayerItem(string playerIP, string state)
    {
        GameObject a3 = Instantiate(itemGObject3) as GameObject;
        GameObject a4 = Instantiate(itemGObject4) as GameObject;
        a3.transform.SetParent(content3GObject.transform);
        a4.transform.SetParent(content3GObject.transform);
        a3.transform.localPosition = new Vector3(itemLocalPos3.x, itemLocalPos3.y - (items.Count + 1) / 2 * itemHeight3, 0);
        a4.transform.localPosition = new Vector3(itemLocalPos3.x, itemLocalPos3.y - (items.Count + 1) / 2 * itemHeight3, 0);
        a3.GetComponent<Text>().text = playerIP;
        a4.GetComponent<Text>().text = state;
        items.Add(a3);
        items.Add(a4);
        if (content3Size.y <= ((items.Count + 1) / 2) * itemHeight3)
        {
            content3GObject.GetComponent<RectTransform>().sizeDelta = new Vector2(content3Size.x, (items.Count + 1) / 2 * itemHeight3);
        }
    }
    #endregion


    public void OnJoinRoomClick()
    {
        //发送 JoinRoom 协议
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("JoinRoom");
        protocol.AddString(room_tag.GetComponent<Text>().text); //加入所选定的那个房间
        Debug.Log("发送 " + protocol.GetDesc());
        NetMgr.srvConn.Send(protocol);  //这也是个房间内广播，只需要默认分发消息就行了
        //TODO:这里还要分之前是否加入过其他房间
    }
    public void JoinRoom(ProtocolBase protocol)
    {
        ProtocolBytes proto = (ProtocolBytes)protocol;
        int start = 0;
        /*string protoName = */proto.GetString(start, ref start);
        string person_name;

        string roomName = room_tag.GetComponent<Text>().text;
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RoomName.Equals(roomName))
            {
                currentRoom = roomList[i];  //将找到的这个房间设为当前房间
                roomList[i].PlayerList.Clear(); //先将房间列表中的所有的玩家都删掉重新获取
                while ((person_name = proto.GetString(start, ref start)) != "")
                {
                    Debug.Log("Join :"+person_name);
                    roomList[i].PlayerList.Add(new Player(person_name));    //重新得到了一个 PlayerList
                }
                //先清空 content
                createButton.transform.parent.Find("PlayerInRoomList").GetComponent<ScrollRect>().content.DetachChildren();
                items.Clear();
                //重新渲染 列表2
                //在 list 中的第一个的就是房主  所以跳过第一个
                AddEachPlayerItem(roomList[i].PlayerList[0].Name, "host");
                for (int n = 1; n < roomList[i].PlayerList.Count; n++)
                    AddEachPlayerItem(roomList[i].PlayerList[n].Name, "");
            }
        }

    }
    public void OnBeginGameClick()
    {
        if (!Root.this_player.Name.Equals(currentRoom.PlayerList[0].Name))   //第一个总是房主
        {
            UnityEditor.EditorUtility.DisplayDialog("警告", "你不是队长不能开始", "确认", "取消");
            return;
        }
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("StartFight");
        NetMgr.srvConn.Send(protocol);  //房间内广播，默认分发消息就行了

    }
    public void StartFight(ProtocolBase protocol)
    {
        Debug.Log("我进来了");
        ProtocolBytes proto = (ProtocolBytes)protocol;
        GameObject.Destroy(createButton.transform.parent.parent.parent.gameObject);
        MultiBattle.instance.StartBattle(proto);
    }


    public void OnLeaveRoomClick()
    {
        //发送 LeaveRoom 协议
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("LeaveRoom");
        //离开房间  服务器的每个 conn 连接中保存了所在的房间
        Debug.Log("发送 " + protocol.GetDesc());
        NetMgr.srvConn.Send(protocol);
        //这是一个广播，只需要默认分发消息就行了
        // 这里需要分房主还是非房主   房主离开 将会选举出新的房主；非房主离开服务器 连接指针集 剔除那个人的连接指针；返回房间里的所有人，然后list2重新渲染
    }
    public void LeaveRoom(ProtocolBase protocol)
    {
        ProtocolBytes proto = (ProtocolBytes)protocol;
        int start = 0;
        /*string protoName = */proto.GetString(start, ref start);

        string person_name;

        string roomName = room_tag.GetComponent<Text>().text;
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RoomName.Equals(roomName))  //找到这个房间
            {
                roomList[i].PlayerList.Clear(); //先将房间列表中的所有的玩家都删掉重新获取
                while ((person_name = proto.GetString(start, ref start)) != "")
                {
                    Debug.Log("Leave :" + person_name);
                    roomList[i].PlayerList.Add(new Player(person_name));    //重新得到了一个 PlayerList
                }
                if(roomList[i].PlayerList!=null&& roomList[i].PlayerList.Count > 0)
                {
                    //先清空 content
                    createButton.transform.parent.Find("PlayerInRoomList").GetComponent<ScrollRect>().content.DetachChildren();
                    items.Clear();  //先将存储所有 GameObject 的items 完全清空 然后准备重新装入

                    //重新渲染 列表2
                    //在 list 中的第一个的就是房主  所以跳过第一个
                    AddEachPlayerItem(roomList[i].PlayerList[0].Name, "host");
                    for (int n = 1; n < roomList[i].PlayerList.Count; n++)
                        AddEachPlayerItem(roomList[i].PlayerList[n].Name, "");
                }else
                    return;
            }
        }
        
    }
    #endregion
}

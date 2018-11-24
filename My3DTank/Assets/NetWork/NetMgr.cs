using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//网络管理
public class NetMgr
{
    public static Connection srvConn = new Connection();
    //public static Connection platformConn = new Connection();
    public static void Update()
    {
        srvConn.Update();
        //platformConn.Update();
    }

    //心跳
    public static ProtocolBase GetHeartBeatProtocol()
    {
        //具体的发送内容根据服务端设定改动
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("HeartBeat");
        return protocol;
    }
	//TODO: 在Root中的Update 调用 书P314页
}
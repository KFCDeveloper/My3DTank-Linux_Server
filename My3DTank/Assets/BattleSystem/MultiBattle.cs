using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChobiAssets.KTP;

public class MultiBattle : MonoBehaviour
{
    //单例
    public static MultiBattle instance;
    //坦克预设
    public GameObject tankPrefabs;
    //战场中的所有坦克
    public Dictionary<string, BattleTank> list = new Dictionary<string, BattleTank>();

    // Use this for initialization
    void Start()
    {
        instance = this;
    }

    //清理场景
    public void ClearBattle()
    {
        list.Clear();
        GameObject[] tanks = GameObject.FindGameObjectsWithTag("SD_Tiger-I_1.1");
        for (int i = 0; i < tanks.Length; i++)
        {
            Destroy(tanks[i]);
        }
    }
    //开始战斗
    public void StartBattle(ProtocolBytes proto)
    {
        //一进来就删掉原来的main camera
        GameObject.Destroy(GameObject.Find("Main Camera").gameObject);
        //GameObject tankObj = (GameObject)Instantiate(tankPrefabs);
        //解析协议
        int start = 0;
        string protoName = proto.GetString(start, ref start);
        if (protoName != "StartFight")
            return;
        //坦克总数
        int count = proto.GetInt(start, ref start);
        //TODO:清理场景（我好像不用清理场景）
        //ClearBattle();
        //每一辆坦克
        for (int i = 0; i < count; i++)
        {
            string name = proto.GetString(start, ref start);
            int swopID = proto.GetInt(start, ref start);
            GenerateTank(name, swopID);
        }
        NetMgr.srvConn.msgDist.AddListener("UpdateUnitInfo", RecvUpdateUnitInfo);
        NetMgr.srvConn.msgDist.AddListener("Shooting", RecvShooting);
        NetMgr.srvConn.msgDist.AddListener("Hit", RecvHit);
        NetMgr.srvConn.msgDist.AddListener("Result", RecvResult);
        NetMgr.srvConn.msgDist.AddListener("Blood", RecvBlood);
    }
    //产生坦克
    public void GenerateTank(string name, int swopID)
    {
        Transform sp = GameObject.Find("SwopPoints").transform;
        Transform swopTrans;
        swopTrans = sp.GetChild(swopID);  //给这辆坦克分配一个出生点
        if (swopTrans == null)
        {
            Debug.LogError("GenerateTank 出生点错误");
            return;
        }

        GameObject tankObj = (GameObject)Instantiate(tankPrefabs);
        tankObj.name = name;
        tankObj.transform.position = swopTrans.position;
        tankObj.transform.rotation = swopTrans.rotation;
        //列表处理
        BattleTank bt = new BattleTank();
        bt.tank = tankObj.GetComponent<ID_Control_CS>();
        list.Add(name, bt);

        if (name == Root.this_player.Name)  //找通过名字 筛选本人
        {
            bt.tank.isPlayer = true;
            // CarmeraFollow   书上的是没有相机的，所以需要加上相机，而我的project每个tank自带相机，所以删除其他的相机
        }
        else
        {
            //删除它们的相机
            GameObject.Destroy(bt.tank.transform.Find("MainBody").Find("Camera_Pivot").gameObject);
            bt.tank.isPlayer = false;   //并且不控制
            //bt.tank.ctrlType = Tank.CtrlType.net;
            //初始化位置预测数据，因为 Vector3的默认值为 (0,0,0)
            bt.tank.InitNetCtrl();
        }
    }

    public void RecvUpdateUnitInfo(ProtocolBase protocol)
    {
        //解析协议
        int start = 0;
        ProtocolBytes proto = (ProtocolBytes)protocol;
        string protoName = proto.GetString(start,ref start);
        string name = proto.GetString(start, ref start);
        Vector3 nPos;
        Vector3 nRot;
        nPos.x = proto.GetFloat(start,ref start);
        nPos.y = proto.GetFloat(start,ref start);
        nPos.z = proto.GetFloat(start,ref start);
        nRot.x = proto.GetFloat(start,ref start);
        nRot.y = proto.GetFloat(start,ref start);
        nRot.z = proto.GetFloat(start,ref start);
        float blood = proto.GetFloat(start, ref start);

        float tur_currentAng_in = proto.GetFloat(start,ref start);
        float targetAng_in = proto.GetFloat(start,ref start);
        float canno_current_in = proto.GetFloat(start,ref start);

        Debug.Log("RecvUpdateUnitInfo " + name);
        if (!list.ContainsKey(name))
        {
            Debug.Log("RecvUpdateUnitInfo bt == null ");
            return;
        }
        BattleTank bt = list[name];
        if(name == Root.this_player.Name)   //跳过自己的更新
        {
            return;
        }
        bt.tank.NetForecastInfo(nPos,nRot);        //更新预测位置和旋转
        bt.tank.NetTurretTarget(tur_currentAng_in, targetAng_in, canno_current_in);   //更新炮塔和炮管的旋转角
        bt.tank.NetBlood(blood);
    }
    public void RecvShooting(ProtocolBase protocol)
    {
        //解析协议
        int start = 0;
        ProtocolBytes proto = (ProtocolBytes)protocol;
        string protoName = proto.GetString(start, ref start);
        string name = proto.GetString(start, ref start);
        Vector3 Pos;
        Vector3 Rot;
        Pos.x = proto.GetFloat(start, ref start);
        Pos.y = proto.GetFloat(start, ref start);
        Pos.z = proto.GetFloat(start, ref start);
        Rot.x = proto.GetFloat(start, ref start);
        Rot.y = proto.GetFloat(start, ref start);
        Rot.z = proto.GetFloat(start, ref start);
        Debug.Log("RecvShooting " + name);
        if (!list.ContainsKey(name))
        {
            Debug.Log("RecvShooting bt == null ");
            return;
        }
        BattleTank bt = list[name];
        if (name == Root.this_player.Name)   //跳过自己的更新
        {
            return;
        }
        bt.tank.NetShoot(Pos,Rot);
    }
    public void RecvHit(ProtocolBase protocol)
    {

    }
    public void RecvResult(ProtocolBase protocol)
    {

    }
    public void RecvBlood(ProtocolBase protocol)
    {
        //解析协议
        int start = 0;
        ProtocolBytes proto = (ProtocolBytes)protocol;
        string protoName = proto.GetString(start, ref start);
        string name = proto.GetString(start, ref start);
        float blood = proto.GetFloat(start, ref start);
        Debug.Log("RecvBlood " + name);
        if (!list.ContainsKey(name))
        {
            Debug.Log("RecvBlood bt == null ");
            return;
        }
        BattleTank bt = list[name];
        if (name == Root.this_player.Name)   //跳过自己的更新
        {
            return;
        }
        bt.tank.NetBlood(blood);
    }
}
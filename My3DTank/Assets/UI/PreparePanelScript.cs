using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreparePanelScript : PanelBase
{
    private Button createButton;
    private InputField inputField;
    private new string name;

    #region 生命周期
    public override void Init(params object[] args)
    {
        base.Init(args);
        skinPath = "PreparePanel";
        layer = PanelLayer.PreparePanel;
    }

    public override void OnShowing()
    {
        base.OnShowing();
        Transform skinTrans = skin.transform;
        createButton = skinTrans.Find("CreateButton").GetComponent<Button>();
        inputField = skinTrans.Find("InputField").GetComponent<InputField>();


        createButton.onClick.AddListener(OnCreateName);
    }

    public void OnCreateName()
    {
        name = inputField.text;
        //增强鲁棒性
        if (name.Equals(""))
        {
            UnityEditor.EditorUtility.DisplayDialog("alert", "Please input the name", "确认", "取消");
            return;
        }

        if (NetMgr.srvConn.status != Connection.Status.Connected)
        {
            string host = "192.168.43.23";
            int port = 10000;
            NetMgr.srvConn.proto = new ProtocolBytes();
            NetMgr.srvConn.Connect(host, port);
        }
        //发送
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("Login");
        protocol.AddString(name);
        Debug.Log("发送 " + protocol.GetDesc());
        NetMgr.srvConn.Send(protocol, OnLoginBack);
    }

    public void OnLoginBack(ProtocolBase protocol)
    {
        ProtocolBytes proto = (ProtocolBytes)protocol;
        int start = 0;
        /*string protoName = */proto.GetString(start, ref start);

        int ret = proto.GetInt(start, ref start);

        if (ret == 0)
        {
            Debug.Log("登陆成功");
            Root.this_player.Name = name;
            Close();
            PanelMrg.instance.OpenPanel<FirstPanelScript>("");
            

        }
        else if (ret == 1)
        {
            UnityEditor.EditorUtility.DisplayDialog("alert", "This name has existed", "确认", "取消");
        }
    }
    #endregion
}
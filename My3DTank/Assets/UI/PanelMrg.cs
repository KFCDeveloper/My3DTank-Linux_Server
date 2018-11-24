using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PanelMrg : MonoBehaviour
{
    //单例
    public static PanelMrg instance;
    //画板 canvas
    private GameObject canvas;
    //面板 panel
    public Dictionary<string, PanelBase> dict;
    //层级
    private Dictionary<PanelLayer, Transform> layerDict;

    //开始
    public void Awake()
    {
        instance = this;
        InitLayer();
        dict = new Dictionary<string, PanelBase>();
    }

    //初始化层
    private void InitLayer()
    {
        //画布
        canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("panelMrg InitLayer fail, canvas is null");
        }
        layerDict = new Dictionary<PanelLayer, Transform>();
        foreach (PanelLayer pl in Enum.GetValues(typeof(PanelLayer)))
        {
            string name = pl.ToString();
            Transform transform = canvas.transform.Find(name);
            layerDict.Add(pl, transform);
        }
    }

    //打开面板   
    //这里的 where T:PanelBase 表示 那里面的泛型T都必须是继承自 PanelBase 的
    public void OpenPanel<T>(string skinPath, params object[] args) where T : PanelBase
    {
        //已经打开
        string name = typeof(T).ToString();
        if (dict.ContainsKey(name))
        {
            return;
        }
        //面板脚本
        PanelBase panel = canvas.AddComponent<T>();
        panel.Init(args);
        dict.Add(name, panel);
        //加载皮肤
        skinPath = (skinPath != "" ? skinPath : panel.skinPath);
        GameObject skin = Resources.Load<GameObject>(skinPath);
        if (skin == null)
        {
            Debug.LogError("panelMgr.OpenPanel fail,skin is null,skinPath = " + skinPath);
        }
        panel.skin = (GameObject)Instantiate(skin);
        //坐标
        Transform skinTrans = panel.skin.transform;
        PanelLayer layer = panel.layer;
        Transform parent = layerDict[layer];
        skinTrans.SetParent(parent, false);
        //panel的声明周期
        panel.OnShowing();
        //animation 预留了结束后可以让样板飞离的地方
        panel.OnShowed();
    }

    //关闭面板
    public void ClosePanel(string name)
    {
        PanelBase panel = (PanelBase)dict[name];
        if (panel == null)
        {
            return;
        }
        panel.OnClosing();
        dict.Remove(name);
        panel.OnClosed();
        GameObject.Destroy(panel.skin);
        Component.Destroy(panel);
    }
}


///分层类型
public enum PanelLayer
{
    //准备面板
    PreparePanel,
    //主面板
    FirstPanel
}
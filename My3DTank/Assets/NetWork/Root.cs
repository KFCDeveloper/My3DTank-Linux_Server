using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Root : MonoBehaviour {
	public static Player this_player;
	// Use this for initialization
	void Start () {
        this_player = new Player(null);
		PanelMrg.instance.OpenPanel<PreparePanelScript>("");
	}
	
	// Update is called once per frame
	void Update () {
        NetMgr.Update();
    }
}

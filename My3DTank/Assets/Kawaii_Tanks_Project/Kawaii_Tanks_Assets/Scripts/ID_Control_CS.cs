using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

// This script must be attached to the top object of the tank.
namespace ChobiAssets.KTP
{

    public class ID_Control_CS : MonoBehaviour
    {

        [Header("ID settings")]
        [Tooltip("ID number")] public int id = 0;

        [HideInInspector] public bool isPlayer; // Referred to from child objects.
        [HideInInspector] public Game_Controller_CS controllerScript;
        [HideInInspector] public TankProp storedTankProp; // Set by "Game_Controller_CS".

        [HideInInspector] public Turret_Control_CS turretScript;
        [HideInInspector] public Camera_Zoom_CS mainCamScript;
        [HideInInspector] public GunCamera_Control_CS gunCamScript;

        //last 上次的位置信息
        Vector3 lPos;
        Vector3 lRot;
        //forecast 预测的位置信息
        Vector3 fPos;
        Vector3 fRot;
        //时间间隔
        float delta = 1;
        //上次接收时间
        float lastRecvInfoTime = float.MinValue;
        //网络同步
        private float lastSendInfoTime = float.MinValue;
        float tur_currentAng;
        float tur_targetAng;
        float canno_current;
        //子弹预设
        public GameObject bulletPrefabs;

        void Start()
        {   // Do not change to "Awake ()".
            // Send this reference to the "Game_Controller" in the scene.
            GameObject gameController = GameObject.FindGameObjectWithTag("GameController");
            if (gameController)
            {
                controllerScript = gameController.GetComponent<Game_Controller_CS>();
            }
            if (controllerScript)
            {
                controllerScript.Receive_ID(this);
            }
            else
            {
                Debug.LogError("There is no 'Game_Controller' in the scene.");
            }
            // Broadcast this reference.
            BroadcastMessage("Get_ID_Script", this, SendMessageOptions.DontRequireReceiver);
        }

#if !UNITY_ANDROID && !UNITY_IPHONE
        [HideInInspector] public bool aimButton;
        [HideInInspector] public bool aimButtonDown;
        [HideInInspector] public bool aimButtonUp;
        [HideInInspector] public bool dragButton;
        [HideInInspector] public bool dragButtonDown;
        [HideInInspector] public bool fireButton;
        void Update()
        {
            if (isPlayer)   //是玩家的坦克自己操控
            {
                aimButton = Input.GetKey(KeyCode.Space);
                aimButtonDown = Input.GetKeyDown(KeyCode.Space);
                aimButtonUp = Input.GetKeyUp(KeyCode.Space);
                dragButton = Input.GetMouseButton(1);
                dragButtonDown = Input.GetMouseButtonDown(1);
                fireButton = Input.GetMouseButton(0);

                //50ms 发送自己的同步信息
                if (Time.time - lastSendInfoTime > 0.05f)
                {
                    SendUnitInfo();
                    //SendBloodInfo();
                    lastSendInfoTime = Time.time;
                }
            }
            else if (isPlayer == false)           //非玩家的坦克由网络来同步
            {
                NetUpdate();
            }

        }
        private void LateUpdate()
        {
            if (isPlayer)
            {
                if (fireButton && GameObject.Find("Bullet(Clone)") != null) //坦克开火了
                {
                    bulletPrefabs = GameObject.Find("Bullet(Clone)").GetComponent<Bullet_Nav_CS>().gameObject;
                    SendShootInfo(bulletPrefabs.transform);
                }
            }
        }
#endif

        void Destroy()
        { // Called from "Damage_Control_CS".
            gameObject.tag = "Finish";
        }

        public void Get_Current_ID(int currentID)
        { // Called from "Game_Controller_CS".
            if (id == currentID)
            {
                isPlayer = true;
            }
            else
            {
                isPlayer = false;
            }
            // Call Switch_Player.
            turretScript.Switch_Player(isPlayer);
            mainCamScript.Switch_Player(isPlayer);
            gunCamScript.Switch_Player(isPlayer);
        }

        #region 所有的同步
        public void NetUpdate()
        {
            //当前位置
            Vector3 pos = transform.Find("MainBody").position;
            Vector3 rot = transform.Find("MainBody").eulerAngles;
            //更新位置
            if (delta > 0)
            {
                transform.Find("MainBody").position = Vector3.Lerp(pos, fPos, delta);
                transform.Find("MainBody").rotation = Quaternion.Lerp(Quaternion.Euler(rot), Quaternion.Euler(fRot), delta);

                transform.Find("MainBody").Find("Turret_Base").Find("Cannon_Base").GetComponent<Cannon_Control_CS>().currentAng = canno_current;
                transform.Find("MainBody").Find("Turret_Base").GetComponent<Turret_Control_CS>().currentAng = tur_currentAng;
                transform.Find("MainBody").Find("Turret_Base").GetComponent<Turret_Control_CS>().targetAng = tur_targetAng;
            }
        }
        #endregion
        #region 位置同步处理
        public void SendUnitInfo()
        {
            ProtocolBytes proto = new ProtocolBytes();
            proto.AddString("UpdateUnitInfo");
            //位置旋转
            Vector3 pos = transform.Find("MainBody").position;
            Vector3 rot = transform.Find("MainBody").eulerAngles;
            proto.AddFloat(pos.x);
            proto.AddFloat(pos.y);
            proto.AddFloat(pos.z);
            proto.AddFloat(rot.x);
            proto.AddFloat(rot.y);
            proto.AddFloat(rot.z);
            float blood = transform.GetComponent<Damage_Control_CS>().durability;
            proto.AddFloat(blood);

            //炮塔
            float tur_currentAng_out = transform.Find("MainBody").Find("Turret_Base").GetComponent<Turret_Control_CS>().currentAng;
            float tur_targetAng_out = transform.Find("MainBody").Find("Turret_Base").GetComponent<Turret_Control_CS>().targetAng;
            proto.AddFloat(tur_currentAng_out);
            proto.AddFloat(tur_targetAng_out);
            //炮管
            float canno_current_out = transform.Find("MainBody").Find("Turret_Base").Find("Cannon_Base").GetComponent<Cannon_Control_CS>().currentAng;
            proto.AddFloat(canno_current_out);
            NetMgr.srvConn.Send(proto);
        }

        public void NetForecastInfo(Vector3 nPos, Vector3 nRot)
        {
            //预测的位置
            fPos = lPos + (nPos - lPos) * 2;
            fRot = lRot + (nRot - lRot) * 2;
            //if (Time.time - lastRecvInfoTime > 0.3f)
            //{
            //    fPos = nPos;
            //    fRot = nRot;
            //}
            //时间
            delta = Time.time - lastRecvInfoTime;
            //更新
            fPos = nPos;
            fRot = nRot;
            lastRecvInfoTime = Time.time;
        }

        public void NetTurretTarget(float tur_currentAng_in, float tur_targetAng_in, float canno_current_in)
        {
            tur_currentAng = tur_currentAng_in;
            tur_targetAng = tur_targetAng_in;
            canno_current = canno_current_in;
        }

        public void InitNetCtrl()
        {
            lPos = transform.Find("MainBody").position;
            lRot = transform.Find("MainBody").eulerAngles;
            fPos = transform.Find("MainBody").position;
            fRot = transform.Find("MainBody").eulerAngles;
            //冻结constraints,使物理系统不对坦克产生影响
            Rigidbody r = transform.Find("MainBody").GetComponent<Rigidbody>();
            r.constraints = RigidbodyConstraints.FreezeAll;
        }


        #endregion
        #region 发射炮弹同步
        public void SendShootInfo(Transform bulletTrans)
        {
            ProtocolBytes proto = new ProtocolBytes();
            proto.AddString("Shooting");
            //位置旋转
            Vector3 pos = bulletTrans.position;
            Vector3 rot = bulletTrans.eulerAngles;
            proto.AddFloat(pos.x);
            proto.AddFloat(pos.y);
            proto.AddFloat(pos.z);
            proto.AddFloat(rot.x);
            proto.AddFloat(rot.y);
            proto.AddFloat(rot.z);
            NetMgr.srvConn.Send(proto);
        }
        public void NetShoot(Vector3 Pos, Vector3 Rot)
        {
            transform.Find("MainBody").Find("Turret_Base").Find("Cannon_Base").GetComponent<Fire_Control_CS>().Fire();
            //产生炮弹
            //GameObject bulletObj = (GameObject)Instantiate(bulletPrefabs,Pos,Quaternion.Euler(Rot));
            //Bullet_Nav_CS bulletCmp = bulletObj.GetComponent<Bullet_Nav_CS>();
        }
        #endregion

        #region 血量同步
        public void SendBloodInfo()
        {
            ProtocolBytes proto = new ProtocolBytes();
            proto.AddString("Blood");
            //durability
            float blood = transform.GetComponent<Damage_Control_CS>().durability;
            proto.AddFloat(blood);

            NetMgr.srvConn.Send(proto);
        }
        public void NetBlood(float blood)
        {
            transform.GetComponent<Damage_Control_CS>().durability = blood;
        }
        #endregion
    }
}

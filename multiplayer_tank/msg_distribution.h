#ifndef _MSG_DISTRIBUTION_H__
#define _MSG_DISTRIBUTION_H__

#include "protocol_bytes.h"
#include "data_manager.h"
#include "room_mgr.h"
#include "room.h"
#include <iostream>
extern "C"{
	#include <stdio.h>
	#include <time.h>
}
using namespace std;

void MsgHeartBeat(Conn * conn,ProtocolBase * protobase){
	ProtocolBytes* proto = new ProtocolBytes();
	time_t t;
    t = time(NULL);
    conn->last_ticktime = time(&t);
    cout <<"[Update HeartBeat]:" << conn->last_ticktime << endl;
    proto->add_string((char*)"HeartBeat");
    conn->change_mod = false;

}
void MsgLogin(Conn * conn,ProtocolBase * protobase){
	ProtocolBytes* proto = new ProtocolBytes();
	int start = 0;
	/*char * proto_name = */((ProtocolBytes*)protobase)->get_string(start,&start);
	char * name = ((ProtocolBytes*)protobase)->get_string(start,&start);

	proto->add_string((char*)"Login");
	DataManager* data_manager = DataManager::get_instance();
	if(data_manager->CanRegister(name)){
		//可以写入数据库 并且将其保存进入conn
		conn->name = name;
		data_manager->InsertName(name);
		proto->add_int(0);
	}else{
		proto->add_int(1);
	}
	conn->proto_out = proto;
}

void MsgCreateRoom(Conn * conn,ProtocolBase * protobase){
	int start = 0;
	((ProtocolBytes*)protobase)->get_string(start,&start);
	char * room_name = ((ProtocolBytes*)protobase)->get_string(start,&start);
	conn->tempdata->room_name = room_name;
	RoomMgr* room_mgr = RoomMgr::get_instance();
	Room* created_room = room_mgr->CreateRoom(conn);
	//开始描述内容
	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"CreateRoom");

	proto->add_string(created_room->room_name);
	proto->add_int(created_room->total_players);
	//广播到所有用户
	ServNet::get_instance()->BroadCastAll(proto);

	conn->change_mod = false;	//这个是广播消息，不用注册，直接回发所有人
}
void MsgAskForRooms(Conn * conn,ProtocolBase * protobase){
	//开始描述内容
	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"AskForRooms");

	RoomMgr* room_mgr = RoomMgr::get_instance();
	for(int i = 0;i<room_mgr->max_rooms;i++){
		if(room_mgr->room_set[i]!=NULL){
			proto->add_string(room_mgr->room_set[i]->room_name);
			proto->add_int(room_mgr->room_set[i]->total_players);
		}
	}
	conn->proto_out = proto;
}
void MsgAskForSpeRoom(Conn * conn,ProtocolBase * protobase){
	int start = 0;
	((ProtocolBytes*)protobase)->get_string(start,&start);
	char * room_name = ((ProtocolBytes*)protobase)->get_string(start,&start);
	RoomMgr* room_mgr = RoomMgr::get_instance();

	Room* spe_room = room_mgr->FindByName(room_name);
	//开始描述内容
	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"AskForSpeRoom");
	for(int i = 0;i<spe_room->max_players;i++){
		if(spe_room->conn_set[i]!=NULL){
			proto->add_string(spe_room->conn_set[i]->name);
		}
	}
	conn->proto_out = proto;
}
void MsgJoinRoom(Conn * conn,ProtocolBase * protobase){
	int start = 0;
	((ProtocolBytes*)protobase)->get_string(start,&start);
	char * room_name = ((ProtocolBytes*)protobase)->get_string(start,&start);
	RoomMgr* room_mgr = RoomMgr::get_instance();

	Room* spe_room = room_mgr->FindByName(room_name);
	spe_room->AddConn(conn);
	//开始描述内容
	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"JoinRoom");
	for(int i = 0;i<spe_room->max_players;i++){
		if(spe_room->conn_set[i]!=NULL){
			proto->add_string(spe_room->conn_set[i]->name);
		}
	}
	spe_room->BroadCast(proto);
	conn->change_mod = false;	//这个是广播消息，不用注册，直接回发所有人
}
void MsgLeaveRoom(Conn * conn,ProtocolBase * protobase){
	int start = 0;
	((ProtocolBytes*)protobase)->get_string(start,&start);
	RoomMgr* room_mgr = RoomMgr::get_instance();

	Room* spe_room = room_mgr->FindByName(conn->tempdata->room_name);
	spe_room->DelConn(conn);
	//开始描述内容
	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"LeaveRoom");
	for(int i = 0;i<spe_room->max_players;i++){
		if(spe_room->conn_set[i]!=NULL){
			proto->add_string(spe_room->conn_set[i]->name);
		}
	}
	spe_room->BroadCast(proto);
	conn->Send(proto);	//由于 房间中的指针被置空了，所以需要单独对这个连接发消息
	conn->change_mod = false;	//这个是广播消息，不用注册，直接回发所有人
}
void MsgStartFight(Conn * conn,ProtocolBase * protobase){
	int start = 0;
	((ProtocolBytes*)protobase)->get_string(start,&start);
	//直接广播，前端已经帮忙校验了
	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"StartFight");
	RoomMgr* room_mgr = RoomMgr::get_instance();
	//找到这个房间
	Room* spe_room = room_mgr->FindByName(conn->tempdata->room_name);
	proto->add_int(spe_room->total_players);
	for(int i = 0;i<15;i++){
		if(spe_room->conn_set[i]!=NULL){//找到里面的人
			spe_room->conn_set[i]->tempdata->hp = 300;
			spe_room->conn_set[i]->tempdata->status = 2;
			proto->add_string(spe_room->conn_set[i]->name);
			proto->add_int(i);
		}
	}

	spe_room->BroadCast(proto);
	conn->change_mod = false;	//这个是广播消息，不用注册，直接回发所有人
}
void MsgUpdateUnitInfo(Conn * conn,ProtocolBase * protobase){
	RoomMgr* room_mgr = RoomMgr::get_instance();
	//找到这个房间
	Room* spe_room = room_mgr->FindByName(conn->tempdata->room_name);

	int start = 0;
	((ProtocolBytes*)protobase)->get_string(start,&start);
	float posX = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float posY = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float posZ = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float rotX = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float rotY = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float rotZ = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float blood = ((ProtocolBytes*)protobase)->get_float(start,&start);
	
	float gunRot = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float guncurrent = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float gunRoll = ((ProtocolBytes*)protobase)->get_float(start,&start);
	conn->tempdata->poX = posX;
	conn->tempdata->poY = posY;
	conn->tempdata->poZ = posZ;

	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"UpdateUnitInfo");

	proto->add_string(conn->name);
	proto->add_float(posX);
	proto->add_float(posY);
	proto->add_float(posZ);
	proto->add_float(rotX);
	proto->add_float(rotY);
	proto->add_float(rotZ);
	proto->add_float(blood);
	
	proto->add_float(gunRot);
	proto->add_float(guncurrent);
	proto->add_float(gunRoll);

	spe_room->BroadCast(proto);
	conn->change_mod = false;	//这个是广播消息，不用注册单独回发
}
void MsgShooting(Conn * conn,ProtocolBase * protobase){
	RoomMgr* room_mgr = RoomMgr::get_instance();
	//找到这个房间
	Room* spe_room = room_mgr->FindByName(conn->tempdata->room_name);

	int start = 0;
	((ProtocolBytes*)protobase)->get_string(start,&start);
	float posX = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float posY = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float posZ = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float rotX = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float rotY = ((ProtocolBytes*)protobase)->get_float(start,&start);
	float rotZ = ((ProtocolBytes*)protobase)->get_float(start,&start);

	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"Shooting");

	proto->add_string(conn->name);
	proto->add_float(posX);
	proto->add_float(posY);
	proto->add_float(posZ);
	proto->add_float(rotX);
	proto->add_float(rotY);
	proto->add_float(rotZ);

	spe_room->BroadCast(proto);
	conn->change_mod = false;	//这个是广播消息，不用注册单独回发

}
void MsgBlood(Conn * conn,ProtocolBase * protobase){
	RoomMgr* room_mgr = RoomMgr::get_instance();
	//找到这个房间
	Room* spe_room = room_mgr->FindByName(conn->tempdata->room_name);
	int start = 0;
	((ProtocolBytes*)protobase)->get_string(start,&start);
	float blood = ((ProtocolBytes*)protobase)->get_float(start,&start);
	
	ProtocolBytes* proto = new ProtocolBytes();
	proto->add_string((char*)"Blood");
	proto->add_string(conn->name);
	proto->add_float(blood);
	
	spe_room->BroadCast(proto);
	conn->change_mod = false;	//这个是广播消息，不用注册单独回发
}
void MsgWrong(Conn * conn,ProtocolBase * protobase){
	cout<<"Protocol Name is not matched"<<endl;
}

#endif

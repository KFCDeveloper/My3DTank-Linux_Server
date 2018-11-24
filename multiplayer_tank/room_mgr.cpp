extern "C"{
	#include <stdio.h>
	#include <string.h>
	#include <stdlib.h>
}
#include <iostream>
#include "room_mgr.h"
using namespace std;

RoomMgr::RoomMgr(){
	max_rooms = 10;
	room_set = new Room*[max_rooms];
	for(int i = 0;i<max_rooms;i++){	//此时将所有的指针都置为0
		room_set[i] = NULL;
	}
}

RoomMgr* RoomMgr::instance = new RoomMgr();
RoomMgr* RoomMgr::get_instance(){
	return instance;
}
//创建房间
Room* RoomMgr::CreateRoom(Conn*conn){
	for(int i = 0;i<max_rooms;i++){	//此时将所有的指针都置为0
		if(room_set[i] == NULL){
			room_set[i] = new Room();		//初始化一个房间
			strcpy(room_set[i]->room_name,conn->tempdata->room_name);	//修改房间的名字
			char * a = room_set[i]->room_name;
			room_set[i]->AddConn(conn);
			return room_set[i];
		}
	}
	return NULL;
}
//find room by name
Room* RoomMgr::FindByName(char * input_room_name){
	for(int i = 0;i<max_rooms;i++){
		if(strcmp(room_set[i]->room_name,input_room_name)==0){
			return room_set[i];
		}
	}
	return NULL;
}

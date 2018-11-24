#ifndef _ROOM_MGR_H__
#define _ROOM_MGR_H__

extern "C"{
	#include <stdio.h>
	#include <string.h>
	#include <stdlib.h>
}
#include <iostream>
#include "room.h"
using namespace std;
class RoomMgr{
	public:
		RoomMgr();
		~RoomMgr();
		//房间管理类的单例
		static RoomMgr * instance;
		//房间的最大数量
		int max_rooms;
		//房间集  在创建房间的时候初始化里面的值
		Room** room_set;

		//return an instance
		static RoomMgr* get_instance();

		//创建房间
		Room* CreateRoom(Conn*conn);
		//find room by name
		Room* FindByName(char * input_room_name);

};
#endif

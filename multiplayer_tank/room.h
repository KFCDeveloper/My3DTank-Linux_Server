#ifndef _ROOM_H__
#define _ROOM_H__

extern "C"{
	#include <stdio.h>
	#include <string.h>
	#include <stdlib.h>
}
#include <iostream>
#include "conn.h"
using namespace std;
class Room{
	public:
		Room();
		~Room();
		//房间的状态 1 为准备 2 为开战中
		int status;
		//最大人数
		int max_players;
		//总人数
		int total_players;
		//房间连接池  第一个总是房主
		Conn** conn_set;
		//房间的名字  在创建房间里面初始化
		char * room_name;

		//增加玩家 (并入一个连接)
		bool AddConn(Conn *);
		//删除玩家 (将指定的一个连接指针集中的一个指针置为空)
		void DelConn(Conn *);
		//广播
		void BroadCast(ProtocolBase*);
};
#endif

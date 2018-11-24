extern "C"{
	#include <stdio.h>
	#include <string.h>
	#include <stdlib.h>
	#include <sys/time.h>
}
#include <iostream>
#include "room.h"
#include "serv_net.h"

using namespace std;
Room::Room(){
	status = 1;
	max_players = 15;
	total_players = 0;
	conn_set = new Conn*[max_players];
	for(int i = 0;i<max_players;i++){	//将连接全部置空
		conn_set[i] = NULL;
	}
	room_name = new char[30];
	memset(room_name,'\0',30);
}
Room::~Room(){}

//增加玩家 (并入一个连接)
bool Room::AddConn(Conn * conn){
	setitimer(ITIMER_REAL,NULL,&(ServNet::itv));	//取消时钟
	bool can_add = false;
	for(int i = 0;i<max_players;i++){	//检测还有没有没有指向一个连接的指针
		if(conn_set[i]==NULL){
			conn_set[i] = conn;
			strcpy(conn_set[i]->tempdata->room_name,room_name);	//设置房间名
			total_players = total_players+1;	//人数加1
			can_add = true;
			return can_add;
		}
	}
	setitimer(ITIMER_REAL,&(ServNet::itv),NULL);	//恢复时钟
	return can_add;
}
//删除玩家（将指定的一个连接指针集中的一个指针置为空）
void Room::DelConn(Conn * conn){
	setitimer(ITIMER_REAL,NULL,&(ServNet::itv));	//取消时钟
	for(int i = 0;i<max_players;i++){
        char*a = conn_set[i]->name;
		if(strcmp(conn_set[i]->name,conn->name)==0){	//找到那个玩家
			if(i==0){	//房主
				conn_set[0] = NULL;	//剔除指向房主的指针
				conn_set[0]->tempdata->room_name = NULL;
				for(int j = 1;i < max_players;j++){
					if(conn_set[j]!=NULL){	//找到了一个接盘侠
						conn_set[0] = conn_set[j];	//将接盘侠放在最前面
						conn_set[j] = NULL;		//将接盘侠原来的位置置空
						return;
					}
				}
			}else if(i!=0){//不是房主，直接置空
				conn_set[i] = NULL;
				conn_set[i]->tempdata->room_name = NULL;
				return;
			}
		}
	}
	total_players = total_players-1;//房间人数减1
	setitimer(ITIMER_REAL,&(ServNet::itv),NULL);	//恢复时钟
}
//广播
void Room::BroadCast(ProtocolBase* proto){
	for(int i = 0;i<max_players;i++){
		if(conn_set[i]!=NULL){
			conn_set[i]->Send(proto);
		}
	}
}


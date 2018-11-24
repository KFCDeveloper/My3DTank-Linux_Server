#ifndef _CONN_H__
#define _CONN_H__
extern "C"{
	#include <sys/socket.h>
	#include <netinet/in.h>
	#include <netinet/ip.h> /* superset of previous */
	#include <fcntl.h>
	#include <unistd.h>
	#include <stdio.h>
	#include <errno.h>
}
#include <iostream>
#include "protocol_base.h"

using namespace std;
//传输使用的临时数据
struct PlayerTempData{
	//所加入的房间名
	char * room_name;
	//状态 1 准备 2 开战
	int status;

	//战场相关
	double last_update_time;
	float poX;
	float poY;
	float poZ;
	double last_shoot_time;
	int hp;
};
class Conn{
	public:
		int tcp_socket;
		struct sockaddr_in addr;
		//是否使用
		bool is_use;
		//对应玩家的名字
		char * name;
		//ip地址
		char * ip;
		//port
		int port;
		//有的时候不需要注册成写出模式 如HeartBeat
		bool change_mod;


		//read_buffer
		char * read_buffer;
		//buffer count
		int buffer_count;

		//粘包和分包
		char* len_bytes;
		int msg_length;

		//心跳时间
		int last_ticktime;

		//保存接收协议
		ProtocolBase* protocol;
		//保存发出协议
		ProtocolBase* proto_out;
		//需要用到的临时数据
		PlayerTempData* tempdata;

		//constructor
		Conn();
		//destructor
		~Conn();
		//send protocol
		void Send(ProtocolBase*);
		//close the connection
		void Close();


};
#endif

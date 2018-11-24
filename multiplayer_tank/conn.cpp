#include <iostream>
#include "conn.h"
#include "data_manager.h"
#include "protocol_bytes.h"
extern "C"{
    #include <stdio.h>
    #include <stdlib.h>
    #include <string.h>
    #include <time.h>
}
using namespace std;
//constructor
Conn::Conn(){
	read_buffer = new char[1024];
	memset(read_buffer,'\0',1024);
	is_use = false;
	buffer_count = 0;

	len_bytes = new char[1024];
	memset(len_bytes,'\0',1024);
	msg_length = 0;

	//初始化时间戳
	time_t t;
    t = time(NULL);
    last_ticktime = time(&t);

    //初始化 ip字符串
    ip = new char[20];
    memset(ip,'\0',20);
    //初始化玩家的名字
    name = new char[30];
    memset(name,'\0',30);

    //初始化 默认需要改变模式
    change_mod = true;

    //初始化临时数据集
    tempdata = new PlayerTempData();
    //所加入的房间名 加入了房间后才会有值
    tempdata->room_name = new char[30];
    tempdata->status = 1;
    tempdata->last_update_time=0;
    tempdata->poX=0;
    tempdata->poY=0;
    tempdata->poZ=0;
    tempdata->last_shoot_time=0;
    tempdata->hp=0;

}
//destructor
Conn::~Conn(){

}
//send protocol
void Conn::Send(ProtocolBase * proto){
	write(tcp_socket,((ProtocolBytes*)proto)->bytes,((ProtocolBytes*)proto)->bytes_length);
	//不能关闭tcp_socket，关闭就gg了
}
//close the connection
void Conn::Close(){
	if(!is_use)
		return;
	cout<<"[connect over]"<<ip<<":"<<port<<endl;
	close(tcp_socket);
	is_use = false;
	//断开连接的时候顺便 把这个名字从数据库中删除，腾出空位
	DataManager* data_manager = DataManager::get_instance();
	data_manager->DeleteName(this->name);
}

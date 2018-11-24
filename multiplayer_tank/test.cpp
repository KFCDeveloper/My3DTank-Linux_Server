#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <iostream>
#include "protocol_bytes.h"
#include "data_manager.h"
#include "serv_net.h"
#include "room.h"
#include "room_mgr.h"
using namespace std;
int main(){
/**	协议编码解码测试
	ProtocolBytes* pro = new ProtocolBytes();
	char inputchar[] = "0000sdcvewvwvwoipmpoji";
	inputchar[0] = 18;
	inputchar[1] = 0;
	inputchar[2] = 0;
	inputchar[3] = 0;
	pro = (ProtocolBytes*)(pro->decode(inputchar,0,22));

	printf("%s",pro->get_string(0));
    pro->add_double(12.0897832);
    //printf("%f",pro->get_double(22));
*/

/**	数据库管理类测试
    DataManager* manager1 = DataManager::get_instance();
	DataManager* manager2 = DataManager::get_instance();
	if(manager1==manager2)
		printf("instance is created successfully\n");
	if(manager1->CanRegister((char*)"xshh")){
        cout<<"yes"<<endl;
        manager1->InsertName((char*)"xshh");
	}
	//manager1.IsSafeStr("sdfsafafasfdsaf");
*/
/** */
    //RoomMgr* room_mgr  = RoomMgr::get_instance();
	ServNet* serv_net1 = ServNet::get_instance();
	ServNet* serv_net2 = ServNet::get_instance();
	if(serv_net1==serv_net2)
		printf("instance is created successfully\n");
    serv_net1->proto = new ProtocolBytes();
	serv_net1->Start((char*)"192.168.43.23",10000);



	return 0;
}


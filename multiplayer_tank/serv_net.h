#ifndef _SERV_NET_H__
#define _SERV_NET_H__

#include "conn.h"
#include "protocol_base.h"
#include "data_manager.h"
extern "C"{
	#include <sys/socket.h>
	#include <netinet/in.h>
	#include <netinet/ip.h> /* superset of previous */
}
#define MAXLINE 300
#define OPEN_MAX 100
#define LISTENQ 20
#define SERV_PORT 5000
#define INFTIM 1000

class ServNet{
	public:
		//客户端连接池
		Conn** conns;
		//声明一个锁，来锁住 conn

		//使用的协议
		ProtocolBase* proto;

		//最大连接数
		static int max_conn;
		static ServNet* instance;
		static struct itimerval itv;

		//时间戳
		int time_stamp;
		//心跳间隔  与客户端保持一致
		int heartbeat_time;
		//轮询连接看心跳的间隔时间
		int heartbeat_poll_time;

		ServNet();
		~ServNet();
		//return an instance
		static ServNet* get_instance();
		//start server and pass into the host and the port
		void Start(char * host,int port);
		//accept()后的回调函数
		void AcceptCb();
		void Close();

		//发送消息
		void Send(int,Conn*);

		//数据处理的函数 包括粘包分包处理 以及协议处理
		void ProcessData(Conn*);
		//协议分发函数
		void HandleMsg(Conn*,ProtocolBase*);

		//心跳机制
		void HeartBeatMach(int signum);
		//返回未使用索引
		int NewIndex();
		//将套接字设置为非阻塞
		void setnonblocking(int socket);
		//根据得到的 sockfd 返回对应的连接
		Conn* get_index(int arg_sockfd);
		//广播给所有的连接
		void BroadCastAll(ProtocolBase*);
};
#endif

#include "serv_net.h"
#include "conn.h"
#include <iostream>
#include "protocol_bytes.h"
#include "msg_distribution.h"
extern "C"{
    #include <sys/socket.h>
    #include <sys/epoll.h>
    #include <netinet/in.h>
    #include <arpa/inet.h>
    #include <fcntl.h>
    #include <unistd.h>
    #include <stdio.h>
    #include <errno.h>
    #include <strings.h>
    #include <stdlib.h>
    #include <sys/time.h>
    #include <signal.h>
}
using namespace std;
void HeartBeat(int signum);
ServNet::ServNet(){
    ServNet::heartbeat_poll_time = 2;
	//给时钟设置参数
	signal(SIGALRM,HeartBeat);
	itv.it_interval.tv_sec=heartbeat_poll_time;
	itv.it_interval.tv_usec=0;
	itv.it_value.tv_sec=0;
	itv.it_value.tv_usec=50000;
}
struct itimerval ServNet::itv;
ServNet* ServNet::instance = new ServNet();

ServNet* ServNet::get_instance(){
	return instance;
}

int ServNet::max_conn = 50;
void ServNet::Start(char * host,int port){
	//初始化时间戳 与 心跳间隔
	time_t t;
    t = time(NULL);
    ServNet::time_stamp = time(&t);
    ServNet::heartbeat_time = 20;   //与客户端的心跳时间一致

	//设置时钟
	setitimer(ITIMER_REAL,&itv,NULL);
	//初始化连接池
	conns = new Conn*[max_conn];
	for(int i = 0;i < max_conn;i++){
		conns[i] = new Conn();
	}
	//开始 I/O multiplexing(I/O 复用) 的编写
	int i, listenfd, sockfd, epfd, nfds;
	ssize_t n;
	char line[MAXLINE];
	socklen_t clilen;
	struct epoll_event ev,events[20];//声明epoll_event结构体的变量,ev用于注册事件,数组用于回传要处理的事件
	epfd=epoll_create(256);//生成用于处理accept的epoll专用的文件描述符
	struct sockaddr_in clientaddr;
	struct sockaddr_in serveraddr;
	listenfd = socket(AF_INET,SOCK_STREAM,0);
	setnonblocking(listenfd);	//把socket设置为非阻塞方式
	ev.data.fd = listenfd;		//设置与要处理的事件相关的文件描述符
	ev.events = EPOLLIN|EPOLLET;	//设置要处理的事件类型

	epoll_ctl(epfd,EPOLL_CTL_ADD,listenfd,&ev);	//注册epoll事件

	//开始对 serveraddr 进行赋值
	bzero(&serveraddr,sizeof(serveraddr));	//将serveraddr中的东西清除
	serveraddr.sin_family = AF_INET;
	inet_aton(host,&(serveraddr.sin_addr));
	serveraddr.sin_port=htons(port);

	//bind listenfd
	if(bind(listenfd,(sockaddr*)&serveraddr,sizeof(serveraddr))==-1){
		perror("binding is error\n");
		return;
	}
	listen(listenfd,LISTENQ);
	for( ; ; ){
		nfds=epoll_wait(epfd,events,20,500);//等待epoll事件的发生
		for(i=0;i<nfds;i++){	//处理发生的 装到 events[20] 里面的事件
			if(events[i].data.fd == listenfd)//如果新监测到一个SOCKET用户连接到了绑定的SOCKET端口，建立新的连接
			{
				int index = NewIndex();
				cout<<index<<endl;
				conns[index]->tcp_socket = accept(listenfd,(sockaddr*)&clientaddr,&clilen);
				if(conns[index]->tcp_socket<0){
					perror("[tcp_socket < 0]:something error with accept");
					exit(1);
				}
				conns[index]->is_use = true; //使用到了这个conn，将其置为 已使用
				char*str= inet_ntoa(clientaddr.sin_addr);
				strcpy(conns[index]->ip,str);//存入conn
				int port_int= ntohs(clientaddr.sin_port);
				conns[index]->port = port_int;	//存入conn
				cout<< "accept a connection from ["<<str<<":"<<port_int<<"]"<<endl;
				ev.data.fd = conns[index]->tcp_socket;	//设置用于读操作的文件描述符
				ev.events = EPOLLIN|EPOLLET;	//设置用于注测的读操作事件
				epoll_ctl(epfd,EPOLL_CTL_ADD,conns[index]->tcp_socket,&ev);	//注册ev
			}
			/**
			 * 这里使用 位与符号的意义在于，猜测 EPOLLIN 可能将一位 置为1，
			 * 所以如果要判断是否设置了 EPOLLIN 就需要判断这位是否被置上，
			 * 所以使用位与，只有1和这位f与上才能够使整体大于零，
			 * 还有就是，因为服务器都是被动的，所以都是客户端来主动连接服务器，客户端先发消息，
			 * 所以，刚刚连接上的socket就将 给它注册 输入描述符改变的事件，
			 * 然后，得到消息后必然就是发出消息，所以就改变 消息注册变成 输出描述符改变事件
			 */
			else if(events[i].events&EPOLLIN)//如果是已经连接的用户，并且收到数据，那么进行读入。
			{
				cout << "EPOLLIN" << endl;
				if((sockfd=events[i].data.fd)<0)
					continue;
				if((n=read(sockfd,line,MAXLINE))<0){
					if(errno == ECONNRESET){
						close(sockfd);
						events[i].data.fd = -1;
					}else
						cout<<"readline error"<<endl;
				}else if(n == 0){
					close(sockfd);
					events[i].data.fd = -1;
				}
				Conn* this_con = get_index(sockfd);//通过这个socket找到这个 连接
				//TODO:在这里读数据 利用到this_con中分配的内存
                if(n == 0){
					close(sockfd);
					events[i].data.fd = -1;
				}   //在这里老是有n=0的bug
				memcpy(this_con->read_buffer,line+this_con->buffer_count+4,n-4);//从"计数指针"处开始读
				this_con->buffer_count += (n-4);	//读入了(n-4)个字符
				ProcessData(this_con);	//开始处理
				this_con->buffer_count = 0; //这个很重要的 一定一定要重置

				//line[n] = '\0';
				//cout << "read " << line << endl;

				//这样就过滤掉了心跳等 不用写出的协议
				if(this_con->change_mod){
                    ev.data.fd = sockfd;	//设置用于写操作的文件描述符
                    ev.events = EPOLLOUT|EPOLLET;	//设置用于注测的写操作事件
                    epoll_ctl(epfd,EPOLL_CTL_MOD,sockfd,&ev);	//修改sockfd上要处理的事件为EPOLLOUT
				}
				this_con->change_mod = true;

			}
			else if(events[i].events&EPOLLOUT)	// 如果有数据发送
			{
				sockfd = events[i].data.fd;
				Conn* this_con = get_index(sockfd);//通过这个socket找到这个 连接
				//TODO:这个连接里面存储了 上一次发来的协议的名字
				//write(sockfd,line,n);
				write(sockfd,((ProtocolBytes*)(this_con->proto_out))->bytes,((ProtocolBytes*)(this_con->proto_out))->bytes_length);
				ev.data.fd = sockfd;	//设置用于读操作的文件描述符
				ev.events = EPOLLIN|EPOLLET;	//设置用于注测的读操作事件
				epoll_ctl(epfd,EPOLL_CTL_MOD,sockfd,&ev);	//修改sockfd上要处理的事件为EPOLLIN
			}
		}
	}
}

void ServNet::AcceptCb(){

}

void ServNet::Close(){

}

void ServNet::Send(int sockfd,Conn*conn){

}

void ServNet::ProcessData(Conn* conn){
	//长度小于4字节
	if(conn->buffer_count < 4)
		return;
	memcpy(&(conn->msg_length),conn->read_buffer,4);
	//存储的字符长度 小于本应该的长度
	if(conn->buffer_count < conn->msg_length+4)
	{
		return;
	}
	memcpy(conn->len_bytes,conn->read_buffer,conn->msg_length);
	cout << conn->len_bytes<<endl;

	//处理消息
	conn->protocol = proto->decode(conn->read_buffer,0,conn->buffer_count);
	HandleMsg(conn,conn->protocol);
	//TODO：下方的消息前推进应该是用在具体的协议里面的  TODO：推进个屁，受不了了，不处理了
	//int count = conn->buffer_count-conn->msg_length-4;
	//memcpy(conn->len_bytes,conn->read_buffer+4+conn->msg_length,count);
}
void HeartBeat(int signum){
	//TODO:ServNet::get_instance()->HeartBeatMach(signum);
}

void ServNet::HeartBeatMach(int signum){
	//更新时间戳
	time_t t;
    t = time(NULL);
    ServNet::time_stamp = time(&t);

	cout<< "main timer is ready!!!" << endl;
	for(int i = 0;i<max_conn;i++){
		if(conns[i]==NULL)
			continue;
		if(conns[i]->is_use==false)
			continue;
		if((conns[i]->last_ticktime)<time_stamp-heartbeat_time)
		{
			cout<<"[HearBeat Causes Break]"<<conns[i]->ip<<":"<<conns[i]->port<<endl;
			conns[i]->Close();
		}
	}
}
void ServNet::HandleMsg(Conn * conn,ProtocolBase * protobase){
	char * name = protobase->get_name();

	char * method_name = new char[20];
	memset(method_name,'\0',20);

	memcpy(method_name,"Msg",3);
	memcpy(method_name+3,name,strlen(name));

	if(conn==NULL){
	 	cout << "this connection is empty......" << endl;
	 	return;
	}
	//这里使用函数指针进行消息的分发
	void (*f)(Conn*,ProtocolBase*);

	if(strcmp(method_name,"MsgHeartBeat")==0) 			f = MsgHeartBeat;
	else if(strcmp(method_name,"MsgLogin")==0) 			f = MsgLogin;
	else if(strcmp(method_name,"MsgCreateRoom")==0) 	f = MsgCreateRoom;
	else if(strcmp(method_name,"MsgAskForRooms")==0) 	f = MsgAskForRooms;
	else if(strcmp(method_name,"MsgAskForSpeRoom")==0) 	f = MsgAskForSpeRoom;
	else if(strcmp(method_name,"MsgJoinRoom")==0) 		f = MsgJoinRoom;
	else if(strcmp(method_name,"MsgLeaveRoom")==0) 		f = MsgLeaveRoom;
	else if(strcmp(method_name,"MsgStartFight")==0) 	f = MsgStartFight;
	else if(strcmp(method_name,"MsgUpdateUnitInfo")==0) f = MsgUpdateUnitInfo;
	else if(strcmp(method_name,"MsgShooting")==0) 		f = MsgShooting;
	else if(strcmp(method_name,"MsgBlood")==0) 			f = MsgBlood;
	else f=MsgWrong;

	f(conn,protobase);
}
void ServNet::setnonblocking(int socket){
	int opts;
	opts = fcntl(socket,F_GETFL);
	if(opts<0)
	{
		perror("fcntl(socket,F_GETFL)");
		exit(1);
	}
	opts = opts|O_NONBLOCK;
	if(fcntl(socket,F_SETFL,opts)<0)
	{
		perror("fcntl(socket,SETFL,opts)");
		exit(1);
	}
}
int ServNet::NewIndex(){
	if(conns==NULL)
		return -1;
	for(int i=0;i<max_conn;i++)
	{
		if(conns[i]==NULL){	//防止错误而已，实际上都会初始化的
			conns[i] = new Conn();
		}else if(conns[i]->is_use==false){//这个连接没有在使用
			return i;
		}
	}
	return -1;
}
Conn* ServNet::get_index(int arg_sockfd){
	if(conns==NULL)
		return NULL;
	for(int i=0;i<max_conn;i++){
		//没有的连接和没用上的连接不考虑，继续遍历
		if((conns[i]==NULL)||(conns[i]->is_use==false)){
			continue;
		}else{
			if(arg_sockfd==conns[i]->tcp_socket)
				return conns[i];
		}
	}
	return NULL;
}
void ServNet::BroadCastAll(ProtocolBase*proto){
	for(int i = 0;i<max_conn;i++){
		if(conns[i]->is_use){
			conns[i]->Send(proto);
		}
	}
}

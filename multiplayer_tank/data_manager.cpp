#include <iostream>
#include "data_manager.h"
#include <string>
extern "C"{
    #include <stdio.h>
    #include <stdlib.h>
    #include <string.h>
}
//#include <regex>
using namespace std;

DataManager::DataManager(){
    Connect();
}
char *server = (char*)"localhost";
char *user = (char*)"root";
char *password = (char*)"123";
DataManager* DataManager::instance = new DataManager();
DataManager* DataManager::get_instance()
{
    return instance;
}

void DataManager::Connect()
{
    mysql_init(&(DataManager::mysql));
    if (!(DataManager::sock=mysql_real_connect(&(DataManager::mysql),server,user,password,"tank_game",0,NULL,0))){
        fprintf(stderr,"Couldn't connect to engine!\n%s\n\n",mysql_error(&(DataManager::mysql)));
        perror("");
        exit(1);
    }
}
bool DataManager::IsSafeStr(char * str_value)
{
	/*//定义一个正则表达式
	regex rep_pattern("[-|;|,|\/|\(|\)|\[|\]|\{|%|@|\*|!|\']",regex_constants::extended);
	//声明匹配结果变量
	match_results<string::const_iterator> rer_result;
	//进行匹配
	bool b_valid = regex_match(str_value, rer_result, rep_pattern);
	if (b_valid)
	{
    	printf("We have succeed in matching");//匹配成功
    	return true;
	}
	return false;*/
	return true;
}

//是否存在该用户
bool DataManager::CanRegister(char* name)
{
	char qbuf[160];     //存放查询sql语句字符串
	MYSQL_RES *res;		//查询结果集，结构类型
	MYSQL_ROW row;		//存放一行查询结果的字符串数组
	//防止sql注入
	if(!DataManager::IsSafeStr(name))
		return false;
	//开始查询是否重复
	sprintf(qbuf,"select name from player where name = '%s'",name);
	if(mysql_query(DataManager::sock,qbuf)) {
        fprintf(stderr,"Query failed (%s)\n",mysql_error(sock));
        exit(1);
    }
	if (!(res=mysql_store_result(sock))) {
        fprintf(stderr,"Couldn't get result from %s\n", mysql_error(sock));
        exit(1);
    }
	printf("number of fields returned: %d\n",mysql_num_fields(res));
	if ((row = mysql_fetch_row(res))!=NULL) {
        mysql_free_result(res);
        //mysql_close(sock);
        return false;
    }else{
        mysql_free_result(res);
        //mysql_close(sock);
        return true;
    }


}
void DataManager::InsertName(char * name){
    char * insert_str;
	char frontstr[] = "insert into player(name) values('";
	char bracket[] = "')";
	insert_str = strcat(frontstr,name);
	insert_str = strcat(insert_str,bracket);
	//增加mysql数据表的条目
	if(mysql_query(sock,insert_str))
		//打印出错误代码及详细信息
		printf("fail to add entry %d:%s\n",mysql_errno(sock),mysql_error(sock));
	else
		//输出受影响的行数
		printf("succeed to add entry Inserted %lurows\n",(unsigned long)mysql_affected_rows(sock));
}
void DataManager::DeleteName(char * name){
	char delete_str[60] = "delete from player where name='";
	char quotation[] = "'";
	strcat(delete_str,name);
	strcat(delete_str,quotation);
	//删除mysql数据表的条目
	if(mysql_query(sock,delete_str))
		//打印出错误代码及详细信息
		printf("fail to delete entry %d:%s\n",mysql_errno(sock),mysql_error(sock));
	else
		//输出受影响的行数
		printf("succeed to delete entry Delete %lurows\n",(unsigned long)mysql_affected_rows(sock));
}


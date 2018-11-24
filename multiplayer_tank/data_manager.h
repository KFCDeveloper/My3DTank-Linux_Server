#ifndef _DATA_MANAGER_H__
#define _DATA_MANAGER_H__
#include "/usr/include/mysql/mysql.h"
using namespace std;
class DataManager
{
	private:

		MYSQL mysql,*sock;
		MYSQL_RES *res;
		MYSQL_ROW *row;

		static DataManager* instance;
		//define the constructor to be a private function to avoid others to call it;
		DataManager();
	public:
		static DataManager* get_instance();
		void Connect();
		bool IsSafeStr(char*);
        bool CanRegister(char*);
        void InsertName(char*);
        void DeleteName(char*);
};

#endif

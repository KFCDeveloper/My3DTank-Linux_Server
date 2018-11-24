#ifndef _PROTOCOL_BYTES_H__
#define _PROTOCOL_BYTES_H__

#include <iostream>
#include "protocol_base.h"
extern "C"{
	#include <stdio.h>
	#include <string.h>
	#include <stdlib.h>
}


using namespace std;
class ProtocolBytes:public ProtocolBase
{
	public:
		char* bytes;
		int bytes_length;
		//constructor and destructor
		ProtocolBytes(void){
            bytes_length = 0;
		}
		~ProtocolBytes(void){}
		//decoder
		ProtocolBase* decode(char* args_bytes,int start,int length){
			ProtocolBase* protocol = new ProtocolBytes();
			((ProtocolBytes*)protocol)->bytes = new char[length+1];

			memcpy(((ProtocolBytes*)protocol)->bytes,args_bytes+start,length);
			((ProtocolBytes*)protocol)->bytes[length] = '\0';

			((ProtocolBytes*)protocol)->bytes_length = length;
			return protocol;
		}
		//encoder
		char* encode(){
			return bytes;
		}
		//protocol name, it is the first 4 bytes
		char* get_name(){
			return get_string(0);
		}
		//description convert all the char to int
		char* get_desc(){
			char* str = new char[1];	//initialize str in heap
			str[0] = '\0';
			char temp[5];
			if(bytes==NULL) return str;
			for(int i = 0;i < (int)strlen(bytes);i++){
				int b = (int)bytes[i];
				sprintf(temp,"%d",b);
				strcat(str,temp);
			}
			return str;
		}
		//add the string
		void add_string(char * str){
			int len = strlen(str);
			//determine 4 bytes
			int length = 4 + strlen(str) + bytes_length + 1;
			char * total_str = new char[length];
			memset(total_str,'\0',length);

			memcpy(total_str,bytes,bytes_length);
			memcpy(total_str+bytes_length,&len,4);
			memcpy(total_str+bytes_length+4,str,len+1);
			//update the bytes and the length of it
			bytes = total_str;
			bytes_length = length-1;
		}
		//read the string from the specified position
		char * get_string(int start,int* end){
			int len = 0;
			memcpy(&len,bytes+start,4);
			if(bytes_length < start+4)
				return NULL;
			//do not care the correctness of the string
			//judge it then
			//initialize a new string to store the value
			char * str = new char[len+1];
			memset(str,'\0',len+1);
			memcpy(str,bytes+start+4,len);	//start copy from the "start" position and also skim 4 which represent the number of the string's length
			*end = start + 4 + len;
			return str;
		}
		char * get_string(int start){
			int end = 0;
			return get_string(start,&end);
		}
		//Add integer. Because an integer occupies 4 bytes, I will also occupy 4 bytes.
		void add_int(int num){
			int length = 4 + bytes_length;
			char * total = new char[length];
			memset(total,'\0',length);
			memcpy(total,bytes,bytes_length);
			memcpy(total+bytes_length,&num,4);
			bytes = total;
			bytes_length = length;
		}
		int get_int(int start,int* end){
            //the default value 0
			if(bytes_length==0){
				return 0;
			}
			//the default value 0
			if(bytes_length < (start + 4)){
				return 0;
			}


			int return_val = 0;
			memcpy(&return_val,bytes+start,4);
			*end = start + sizeof(int);
			return return_val;
		}
		int get_int(int start){
			int end = 0;
			return get_int(start,&end);
		}
		//Add_float
		void add_float(float num){
			int length = 4 + bytes_length;
			char * total = new char[length];
			memset(total,'\0',length);
			memcpy(total,bytes,bytes_length);
			memcpy(total+bytes_length,&num,5);
			bytes = total;
			bytes_length = length;
		}
		float get_float(int start,int* end){
            //the default value 0
			if(bytes_length==0){
				return 0;
			}
			//the default value 0
			if(bytes_length < start + 4){
				return 0;
			}


			float return_val = 0;
			memcpy(&return_val,bytes+start,sizeof(float));
			*end = start + sizeof(float);
			return return_val;
		}
		float get_float(int start){
			int end = 0;
			return get_float(start,&end);
		}

};

#endif

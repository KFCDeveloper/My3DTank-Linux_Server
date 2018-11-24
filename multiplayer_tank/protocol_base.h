#ifndef _PROTOCOL_BASE_H__
#define _PROTOCOL_BASE_H__

#include <iostream>
#include <stdio.h>
using namespace std;
class ProtocolBase
{
	public:
		virtual ProtocolBase* decode(char*,int,int) = 0;
		virtual char* encode() = 0;
		virtual char* get_name() = 0;
		virtual char* get_desc() = 0;
};

#endif

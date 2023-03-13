/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#include <cstring>
#include <vector>
#include <X11/Xlib.h>
#include <X11/Xatom.h>

#include "X11TouchMultiWindowCommon.h"
#include "X11TouchMultiWindowUtils.h"

// ----------------------------------------------------------------------------
void sendMessage(MessageCallback messageCallback, MessageType messageType, const std::string& message)
{
    if (messageCallback)
	{
		// Allocate char array
		char* cstr = new char[message.length() + 1];
		strncpy(cstr, message.c_str(), message.length() + 1);

		// Dispatch to callback
		messageCallback((int)messageType, cstr);

		// Unalloc char array
		delete[] cstr;
	}
}
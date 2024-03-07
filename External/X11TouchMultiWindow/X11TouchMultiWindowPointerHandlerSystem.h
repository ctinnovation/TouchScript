/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#pragma once

#include <map>
#include <vector>
#include <X11/Xlib.h>
#include <X11/Xatom.h>

#include "X11TouchMultiWindowCommon.h"

class EXPORT_API PointerHandlerSystem
{
	typedef std::map<Window, PointerHandler*> PointerHandlerMap;
	typedef PointerHandlerMap::iterator PointerHandlerMapIterator;
	typedef PointerHandlerMap::const_iterator ConstPointerHandlerMapIterator;

private:
	static PointerHandlerSystem* msInstance;

	Display* mDisplay;
	int mOpcode;
	MessageCallback mMessageCallback;
	std::vector<int> mDeviceIds;
	PointerHandlerMap mPointerHandlers;

public:
	PointerHandlerSystem(MessageCallback messageCallback);
	~PointerHandlerSystem();

	static PointerHandlerSystem* getInstance() { return msInstance; }

	Result initialize();
	Result uninitialize();

	Result createHandler(int targetDisplay, Window window, PointerCallback pointerCallback, void** handle);
	PointerHandler* getHandler(Window window) const;
	const int getNumHandlers() const { return mPointerHandlers.size(); }
	Result destroyHandler(PointerHandler* handler);

	Result processEventQueue();

	Result getWindowsOfProcess(unsigned long  pid, Window** windows, uint* numWindows);
	Result freeWindowsOfProcess(Window* windows);
private:
	void getWindowsOfProcess(Window window, unsigned long pid, Atom atomPID, std::vector<Window>& windows);
};
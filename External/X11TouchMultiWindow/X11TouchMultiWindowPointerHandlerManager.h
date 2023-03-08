#pragma once

#include <map>
#include <X11/Xlib.h>

class PointerHandler;
typedef std::map<Window, PointerHandler*> PointerHandlerMap;
typedef PointerHandlerMap::iterator PointerHandlerMapIterator;

class PointerHandlerManager
{
private:
    static int mLastFrameCount;
public:
    static PointerHandlerMap pointerHandlers;
private:
    PointerHandlerManager();
public:
    static void processEvents(int frameCount);
};
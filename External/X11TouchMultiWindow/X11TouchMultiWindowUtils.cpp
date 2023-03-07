/*
@author Jorrit de Vries (jorrit@jorritdevries.com)
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

// ----------------------------------------------------------------------------
void getWindowsOfProcess(Display* display, Window window, unsigned long pid, Atom atomPID, std::vector<Window>& windows)
{
    Atom           type;
    int            format;
    unsigned long  nItems;
    unsigned long  bytesAfter;
    unsigned char *propPID = 0;

    if (XGetWindowProperty(display, window, atomPID, 0, 1, False, XA_CARDINAL,
        &type, &format, &nItems, &bytesAfter, &propPID) == Success)
    {
        if (propPID != 0)
        {
            unsigned long windowPID = *((unsigned long*)propPID);

            if (windowPID == pid)
            {
                windows.push_back(window);
            }

            XFree(propPID);
        }
    }

    // Recurse into window tree
    Window rootWindow;
    Window parentWindow;
    Window* childWindows;
    unsigned numChildWindows;

    if (XQueryTree(display, window, &rootWindow, &parentWindow, &childWindows, &numChildWindows) != 0)
    {
        for (unsigned i = 0; i < numChildWindows; i++)
        {
            getWindowsOfProcess(display, childWindows[i], pid, atomPID, windows);
        }

        if (childWindows)
        {
            XFree(childWindows);
        }
    }
}

// .NET available interface
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result XGetWindowsOfProcess(Display* display, int processPID, Window** windows, uint* numWindows)
{
    if (windows == NULL)
    {
        return Result::ERROR_NULL_POINTER;
    }

    Window defaultRootWindow = XDefaultRootWindow(display);
    Atom atomPID = XInternAtom(display, "_NET_WM_PID", True);
    
    std::vector<Window> result;
    getWindowsOfProcess(display, defaultRootWindow, processPID, atomPID, result);

    *numWindows = result.size();

    // Copy the data to an array
    *windows = new Window[result.size()];
    std::copy(result.begin(), result.end(), *windows);

    return Result::OK;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result XFreeWindowsOfProcess(Window** windows)
{
    if (windows == NULL)
    {
        return Result::ERROR_NULL_POINTER;
    }

    delete[] *windows;

    return Result::OK;
}
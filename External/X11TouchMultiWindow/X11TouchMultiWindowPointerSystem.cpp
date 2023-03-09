/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#include <X11/extensions/XInput2.h>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowPointerSystem.h"

// ----------------------------------------------------------------------------
PointerSytem::PointerSytem(MessageCallback messageCallback)
    : mDisplay(NULL)
    , mOpcode(0)
    , mMessageCallback(messageCallback)
{
    
}
// ----------------------------------------------------------------------------
PointerSystem::~PointerSystem()
{
    // Cleanup remaining handlers
    PointerHandlerMapIterator it;
    for (it = mPointerHandlers.begin(); it != mPointerHandlers.end(); ++it)
    {
        delete it->second;
    }
    mPointerHandlers.clear();


    if (mDisplay != NULL)
    {
        XCloseDisplay(mDisplay);
        mDisplay = NULL;
    }
}
// ----------------------------------------------------------------------------
Result PointerSystem::initialize()
{
    mDisplay = XOpenDisplay(NULL);
    if (mDisplay == NULL)
    {
        sendMessage(mMessageCallback, MessageType::ERROR, "Failed to open X11 display connection.");
        return Result::ERROR_API;
    }

    int event, error;
    if (!XQueryExtension(mDisplay, "XInputExtension", &mOpcode, &event, &error))
    {
        sendMessage(mMessageCallback, MessageType::ERROR, "Failed to get the XInput extension.");

        XCloseDisplay(mDisplay);
        mDisplay = NULL;

        return Result::ERROR_API;
    }

    int major = 2, minor = 3;
    if (XIQueryVersion(dpy, &major, &minor) == BadRequest)
    {
        sendMessage(mMessageCallback, MessageType::Error, "Unsupported XInput extension version: expected 2.3+, actual " +
            std::to_string(major) + "." + std::to_string(minor));

        XCloseDisplay(mDisplay);
        mDisplay = NULL;

        return Result::ERROR_API;
    }

    return Result:OK;
}
// ----------------------------------------------------------------------------
Result PointerSystem::createHandler(Window window, PointerCallback pointerCallback, void** handle)
{
    if (mPointerHandlers.find(window) != mPointerHandlers.end())
    {
        sendMessage(mMessageCallback, MessageType::ERROR, "A handler has already been created for window " + std::to_string(window));
        return Result::ERROR_DUPLICATE_ITEM;
    }

    PointerHandler* handler = new PointerHandler(mDisplay, window, mMessageCallback, pointerCallback);
	*handle = handler;

	mPointerHandlers.insert(std::make_pair(window, handler));
    return handler->initialize();
}
// ----------------------------------------------------------------------------
PointerHandler* PointerSystem::getHandler(Window window) const
{
    return mPointerHandlers[window];
}
// ----------------------------------------------------------------------------
Result PointerSystem::destroyHandler(PointerHandler* handler)
{
    PointerHandlerMapIterator it = mPointerHandlers.find(handler->getWindow());
	if (it != mPointerHandlers.end())
	{
		mPointerHandlers.erase(it);
	}

	delete handler;
	return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerSystem::processEventQueue()
{
    XEvent e;
    while (XEventsQueued(display, QueuedAlready))
    {
        NextEvent(display, &e);
        if (e.type != GenericEvent || e.xcookie.extension != opcode)
        {
            // Received a non xinput event
            continue;
        }

        XIDeviceEvent* xiEvent = (XIDeviceEvent*)e.xcookie.data;
        
        Window window = xiEvent->event;
        PointerHandlerMapIterator it = mPointerHandlers.find(handler->getWindow());
	    if (it == mPointerHandlers.end())
	    {
            sendMessage(mMessageCallback, MessageType::WARNING, "Failed to retrieve handler for window " + std::to_string(window));
            continue;
        }

        it->second->processEvent(xiEvent);
    }

    return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerSystem::getWindowsOfProcess(unsigned long pid, Window** windows, uint* numWindows)
{
    if (windows == NULL)
    {
        return Result::ERROR_NULL_POINTER;
    }

    Window defaultRootWindow = XDefaultRootWindow(mDisplay);
    Atom atomPID = XInternAtom(mDisplay, "_NET_WM_PID", True);
    
    std::vector<Window> result;
    getWindowsOfProcess(defaultRootWindow, processID, atomPID, result);

    *numWindows = result.size();

    // Copy the data to an array
    *windows = new Window[result.size()];
    std::copy(result.begin(), result.end(), *windows);

    return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerSystem::freeWindowsOfProcess(Window* windows)
{
    if (windows == NULL)
    {
        return Result::ERROR_NULL_POINTER;
    }

    delete[] windows;

    return Result::OK;
}
// ----------------------------------------------------------------------------
void PointerSystem::getWindowsOfProcess(Window window, unsigned long pid,
    Atom atomPID, std::vector<Window>& windows)
{
    Atom           type;
    int            format;
    unsigned long  nItems;
    unsigned long  bytesAfter;
    unsigned char *propPID = 0;

    if (XGetWindowProperty(mDisplay, window, atomPID, 0, 1, False, XA_CARDINAL,
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

    if (XQueryTree(mDisplay, window, &rootWindow, &parentWindow, &childWindows, &numChildWindows) != 0)
    {
        for (unsigned i = 0; i < numChildWindows; i++)
        {
            getWindowsOfProcess(mDisplay, childWindows[i], pid, atomPID, windows);
        }

        if (childWindows)
        {
            XFree(childWindows);
        }
    }
}

// .NET available interface
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerSystem_Create(MessageCallback messageCallback,
    void** handle) throw()
{
    PointerSystem* system = new PointerSystem(messageCallback);
	*handle = system;

    return system->initialize();
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerSystem_Destroy(PointerSystem* system) throw()
{
    delete system;
	return Result::OK;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerSystem_CreateHandler(PointerSystem* system,
    Window window, PointerCallback pointerCallback, void** handle) throw()
{
	return system->createHandler(window, pointerCallback, handle);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerSystem_DestroyHandler(PointerSystem* system, PointerHandler* handler) throw()
{
	return system->destroyHandler(handler);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerSystem_ProcessEventQueue(PointerSystem* system)
{
    return system->processEventQueue();
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerSystem_GetWindowsOfProcess(PointerSystem* system,
    int processID, Window** windows, uint* numWindows)
{
    return system->getWindowsOfProcess(procesID, window, numWindows);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result XFreeWindowsOfProcess(PointerSystem* system, Window* windows)
{
    return system->freeWindowsOfProcess(windows);
}
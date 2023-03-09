/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#include <X11/extensions/XInput2.h>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowPointerHandlerSystem.h"
#include "X11TouchMultiWindowUtils.h"

// ----------------------------------------------------------------------------
PointerHandlerSystem::PointerHandlerSystem(MessageCallback messageCallback)
    : mDisplay(NULL)
    , mOpcode(0)
    , mMessageCallback(messageCallback)
{
    
}
// ----------------------------------------------------------------------------
PointerHandlerSystem::~PointerHandlerSystem()
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
Result PointerHandlerSystem::initialize()
{
    sendMessage(mMessageCallback, MessageType::INFO, "Initializing system...");

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
    if (XIQueryVersion(mDisplay, &major, &minor) == BadRequest)
    {
        sendMessage(mMessageCallback, MessageType::ERROR, "Unsupported XInput extension version: expected 2.3+, actual " +
            std::to_string(major) + "." + std::to_string(minor));

        XCloseDisplay(mDisplay);
        mDisplay = NULL;

        return Result::ERROR_API;
    }

    sendMessage(mMessageCallback, MessageType::INFO, "System intialized...");

    return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::createHandler(Window window, PointerCallback pointerCallback, void** handle)
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
PointerHandler* PointerHandlerSystem::getHandler(Window window) const
{
    ConstPointerHandlerMapIterator it = mPointerHandlers.find(window);
    if (it != mPointerHandlers.end())
    {
        return it->second;
    }

    return nullptr;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::destroyHandler(PointerHandler* handler)
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
Result PointerHandlerSystem::processEventQueue()
{
    // XEvent e;
    // while (XEventsQueued(mDisplay, QueuedAlready))
    // {
    //     XNextEvent(mDisplay, &e);
    //     if (e.type != GenericEvent || e.xcookie.extension != mOpcode)
    //     {
    //         // Received a non xinput event
    //         continue;
    //     }

    //     XIDeviceEvent* xiEvent = (XIDeviceEvent*)e.xcookie.data;
        
    //     Window window = xiEvent->event;
    //     PointerHandlerMapIterator it = mPointerHandlers.find(window);
	//     if (it == mPointerHandlers.end())
	//     {
    //         sendMessage(mMessageCallback, MessageType::WARNING, "Failed to retrieve handler for window " + std::to_string(window));
    //         continue;
    //     }

    //     it->second->processEvent(xiEvent);
    // }

    return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::getWindowsOfProcess(unsigned long pid, Window** windows, uint* numWindows)
{
    if (windows == NULL)
    {
        return Result::ERROR_NULL_POINTER;
    }

    Window defaultRootWindow = XDefaultRootWindow(mDisplay);
    Atom atomPID = XInternAtom(mDisplay, "_NET_WM_PID", True);
    
    std::vector<Window> result;
    getWindowsOfProcess(defaultRootWindow, pid, atomPID, result);

    *numWindows = result.size();

    // Copy the data to an array
    *windows = new Window[result.size()];
    std::copy(result.begin(), result.end(), *windows);

    return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::freeWindowsOfProcess(Window* windows)
{
    if (windows == NULL)
    {
        return Result::ERROR_NULL_POINTER;
    }

    delete[] windows;

    return Result::OK;
}
// ----------------------------------------------------------------------------
void PointerHandlerSystem::getWindowsOfProcess(Window window, unsigned long pid,
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
            getWindowsOfProcess(childWindows[i], pid, atomPID, windows);
        }

        if (childWindows)
        {
            XFree(childWindows);
        }
    }
}
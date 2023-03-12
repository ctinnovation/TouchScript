/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#include <X11/extensions/XInput2.h>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowPointerHandlerSystem.h"
#include "X11TouchMultiWindowUtils.h"

PointerHandlerSystem* PointerHandlerSystem::msInstance = nullptr;

// ----------------------------------------------------------------------------
PointerHandlerSystem::PointerHandlerSystem(Display* display, MessageCallback messageCallback)
    : mDisplay(display)
    , mOpcode(0)
    , mMessageCallback(messageCallback)
{
    msInstance = this;
}
// ----------------------------------------------------------------------------
PointerHandlerSystem::~PointerHandlerSystem()
{
    uninitialize();
    msInstance = nullptr;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::initialize()
{
    sendMessage(mMessageCallback, MessageType::MT_INFO, "Initializing system...");

    // We would prefer to check xinput here, but as Unity doesn't load the dependencies
    // properly we do this on the C# side
    // TODO Fix that...

    sendMessage(mMessageCallback, MessageType::MT_INFO, "System intialized");
    return Result::R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::uninitialize()
{
    sendMessage(mMessageCallback, MessageType::MT_INFO, "Uninitializing system...");

    // Cleanup remaining handlers
    PointerHandlerMapIterator it;
    for (it = mPointerHandlers.begin(); it != mPointerHandlers.end(); ++it)
    {
        delete it->second;
    }
    mPointerHandlers.clear();

    sendMessage(mMessageCallback, MessageType::MT_INFO, "System unintialized");
    return Result::R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::createHandler(Window window, PointerCallback pointerCallback, void** handle)
{
    if (mPointerHandlers.find(window) != mPointerHandlers.end())
    {
        sendMessage(mMessageCallback, MessageType::MT_ERROR,
            "A handler has already been created for window " + std::to_string(window));
        return Result::R_ERROR_DUPLICATE_ITEM;
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
	return Result::R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::processEventQueue()
{
    // Flush the output buffer before reading the number of events queued. This
    // is needed as we use QueuedAlready when checking for new events. Using this
    // flag saves a call to flushing, as XNextEvent already flushes the output
    // buffer
    XFlush(mDisplay);

    // The actual processing of the event queue
    XEvent xEvent;
    while (XEventsQueued(mDisplay, QueuedAlready))
    {
        XNextEvent(mDisplay, &xEvent);
        switch (xEvent.type)
        {
            case GenericEvent:
                {
                    if (xEvent.xcookie.extension != mOpcode)
                    {
                        // Received a non xinput event
                        sendMessage(mMessageCallback, MessageType::MT_INFO,
                            "Received event of type " + std::to_string(xEvent.type));
                        continue;
                    }

                    XGetEventData(mDisplay, &xEvent.xcookie);
                    XIDeviceEvent* xiEvent = (XIDeviceEvent*)xEvent.xcookie.data;
                    
                    Window window = xiEvent->event;
                    PointerHandlerMapIterator it = mPointerHandlers.find(window);
                    if (it == mPointerHandlers.end())
                    {
                        XFreeEventData(mDisplay, &xEvent.xcookie);

                        sendMessage(mMessageCallback, MessageType::MT_WARNING,
                            "Failed to retrieve handler for window " + std::to_string(window));
                        continue;
                    }

                    it->second->processEvent(xiEvent);

                    XFreeEventData(mDisplay, &xEvent.xcookie);
                }
            break;
        }
    }

    return Result::R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::getWindowsOfProcess(unsigned long pid, Window** windows, uint* numWindows)
{
    if (windows == NULL)
    {
        return Result::R_ERROR_NULL_POINTER;
    }

    Window defaultRootWindow = XDefaultRootWindow(mDisplay);
    Atom atomPID = XInternAtom(mDisplay, "_NET_WM_PID", True);
    
    std::vector<Window> result;
    getWindowsOfProcess(defaultRootWindow, pid, atomPID, result);

    *numWindows = result.size();

    // Copy the data to an array
    *windows = new Window[result.size()];
    std::copy(result.begin(), result.end(), *windows);

    return Result::R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::freeWindowsOfProcess(Window* windows)
{
    if (windows == NULL)
    {
        return Result::R_ERROR_NULL_POINTER;
    }

    delete[] windows;

    return Result::R_OK;
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
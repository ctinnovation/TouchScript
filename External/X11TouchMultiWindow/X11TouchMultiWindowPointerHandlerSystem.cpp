/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#include <cstring>
#include <X11/extensions/XInput2.h>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowPointerHandlerSystem.h"
#include "X11TouchMultiWindowUtils.h"

PointerHandlerSystem* PointerHandlerSystem::msInstance = nullptr;

// ----------------------------------------------------------------------------
PointerHandlerSystem::PointerHandlerSystem(MessageCallback messageCallback)
    : mDisplay(NULL)
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
    sendMessage(mMessageCallback, MT_INFO, "Initializing system...");

    mDisplay = XOpenDisplay(NULL);
    if (mDisplay == NULL)
    {
        sendMessage(mMessageCallback, MT_ERROR, "Failed to open X11 display connection.");
        return R_ERROR_API;
    }

    int event, error;
    if (!XQueryExtension(mDisplay, "XInputExtension", &mOpcode, &event, &error))
    {
        sendMessage(mMessageCallback, MT_ERROR, "Failed to get the XInput extension.");

        XCloseDisplay(mDisplay);
        mDisplay = NULL;

        return R_ERROR_API;
    }

    int major = 2, minor = 3;
    if (XIQueryVersion(mDisplay, &major, &minor) == BadRequest)
    {
        sendMessage(mMessageCallback, MT_ERROR, "Unsupported XInput extension version: expected 2.3+, actual " +
            std::to_string(major) + "." + std::to_string(minor));

        XCloseDisplay(mDisplay);
        mDisplay = NULL;

        return R_ERROR_API;
    }

    Status status = Success;
    int numDevices;
    XIDeviceInfo* devices = XIQueryDevice(mDisplay, XIAllDevices, &numDevices);
    for (int i = 0; i < numDevices; i++)
	{
		XIDeviceInfo device = devices[i];
		if (device.use == XIMasterPointer || device.use == XISlavePointer || device.use == XIFloatingSlave)
		{
			for (int j = 0; j < device.num_classes; j++)
			{
                XIAnyClassInfo* classInfo = device.classes[j];
				switch (classInfo->type)
				{
					// Touch
					case XITouchClass:
					// Mouse, touchpad
					case XIButtonClass:
					case XIValuatorClass:
                        mDeviceIds.push_back(classInfo->sourceid);
                        break;
                }
            }
        }
    }

    XIFreeDeviceInfo(devices);

    if (status != Success)
	{
		return R_ERROR_UNSUPPORTED;
	}

    // Propagate requests to X server
	XFlush(mDisplay);

    sendMessage(mMessageCallback, MT_INFO, "System intialized with XInput version " +
            std::to_string(major) + "." + std::to_string(minor));
    return R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::uninitialize()
{
    sendMessage(mMessageCallback, MT_INFO, "Uninitializing system...");

    // Cleanup remaining handlers
    PointerHandlerMapIterator it;
    for (it = mPointerHandlers.begin(); it != mPointerHandlers.end(); ++it)
    {
        delete it->second;
    }
    mPointerHandlers.clear();

    sendMessage(mMessageCallback, MT_INFO, "System unintialized");
    return R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::createHandler(int targetDisplay, Window window,
    PointerCallback pointerCallback, void** handle)
{
    if (mPointerHandlers.find(window) != mPointerHandlers.end())
    {
        sendMessage(mMessageCallback, MT_ERROR,
            "A handler has already been created for window " + std::to_string(window));
        return R_ERROR_DUPLICATE_ITEM;
    }

    PointerHandler* handler = new PointerHandler(mDisplay, targetDisplay,
        window, mMessageCallback, pointerCallback);
	*handle = handler;

	mPointerHandlers.insert(std::make_pair(window, handler));
    return handler->initialize(mDeviceIds);
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
	return R_OK;
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
                        // sendMessage(mMessageCallback, MT_INFO,
                        //     "Received event of type " + std::to_string(xEvent.type));
                        continue;
                    }

                    XGetEventData(mDisplay, &xEvent.xcookie);
                    XIDeviceEvent* xiEvent = (XIDeviceEvent*)xEvent.xcookie.data;
                    
                    Window window = xiEvent->event;
                    PointerHandlerMapIterator it = mPointerHandlers.find(window);
                    if (it == mPointerHandlers.end())
                    {
                        XFreeEventData(mDisplay, &xEvent.xcookie);

                        sendMessage(mMessageCallback, MT_WARNING,
                            "Failed to retrieve handler for window " + std::to_string(window));
                        continue;
                    }

                    it->second->processEvent(xiEvent);

                    XFreeEventData(mDisplay, &xEvent.xcookie);
                }
            break;
        }
    }

    return R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::getWindowsOfProcess(unsigned long pid, Window** windows, uint* numWindows)
{
    if (windows == NULL)
    {
        return R_ERROR_NULL_POINTER;
    }

    Window defaultRootWindow = XDefaultRootWindow(mDisplay);
    Atom atomPID = XInternAtom(mDisplay, "_NET_WM_PID", True);
    
    std::vector<Window> result;
    getWindowsOfProcess(defaultRootWindow, pid, atomPID, result);

    *numWindows = result.size();

    // Copy the data to an array
    *windows = new Window[result.size()];
    std::copy(result.begin(), result.end(), *windows);

    return R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandlerSystem::freeWindowsOfProcess(Window* windows)
{
    if (windows == NULL)
    {
        return R_ERROR_NULL_POINTER;
    }

    delete[] windows;

    return R_OK;
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
/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/

#include <cstring>
#include <X11/extensions/XInput2.h>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowPointerHandlerManager.h"
#include "X11TouchMultiWindowUtils.h"

// ----------------------------------------------------------------------------
PointerHandler::PointerHandler(Display* display, Window window, PointerCallback pointerCallback)
    : mDisplay(display)
	, mWindow(window)
	, mPointerCallback(pointerCallback)
	, mXInputOpcode(0)
    , mWidth(0)
	, mHeight(0)
	, mOffsetX(0.0f)
	, mOffsetY(0.0f)
	, mScaleX(1.0f)
	, mScaleY(1.0f)
{

}
// ----------------------------------------------------------------------------
PointerHandler::~PointerHandler()
{

}
// ----------------------------------------------------------------------------
Result PointerHandler::initialize(MessageCallback messageCallback)
{
    sendMessage(messageCallback, MessageType::INFO, "Initializing handler...");

	if (mDisplay == NULL)
	{
		sendMessage(messageCallback, MessageType::ERROR, "'display' is NULL");
		return Result::ERROR_NULL_POINTER;
	}

    if (mWindow == None)
    {
        sendMessage(messageCallback, MessageType::ERROR, "'window' is None");
        return Result::ERROR_NULL_POINTER;
    }

	// Request the opcode for XInput2
	int evt, err;
	if (!XQueryExtension(mDisplay, "XInputExtension", &mXInputOpcode, &evt, &err))
	{
		sendMessage(messageCallback, MessageType::ERROR, "'XInput extension is not available");
		return Result::ERROR_UNSUPPORTED;
	}

	// Setup the event mask fore the events we want to listen to
	unsigned char mask[XIMaskLen(XI_LASTEVENT)];
	memset(mask, 0, sizeof(mask));
	XISetMask(mask, XI_ButtonPress);
	XISetMask(mask, XI_ButtonRelease);
	XISetMask(mask, XI_TouchBegin);
	XISetMask(mask, XI_TouchUpdate);
	XISetMask(mask, XI_TouchEnd);

	XIEventMask eventMask = {
		.deviceid = XIAllDevices, // TODO Only touch devices?
		.mask_len = sizeof(mask),
		.mask = mask
	};

	Status status = XISelectEvents(mDisplay, mWindow, &eventMask, 1);
	if (status != Success)
	{
		sendMessage(messageCallback, MessageType::ERROR, "Failed to select pointer events on window: " + std::to_string(status));
		return Result::ERROR_UNSUPPORTED;
	}

	XFlush(mDisplay);

    return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerHandler::getScreenResolution(MessageCallback messageCallback, int* width, int* height)
{
    // Get the screen for the window
    XWindowAttributes attributes; 
    if (XGetWindowAttributes(mDisplay, mWindow, &attributes) != 0)
    {
        *width = XWidthOfScreen(attributes.screen);
        *height = XHeightOfScreen(attributes.screen);
        return Result::OK;
    }
    else
    {
        sendMessage(messageCallback, MessageType::ERROR, "Failed to retrieve XWindowAttributes");
        return Result::ERROR_API;
    }
}
// ----------------------------------------------------------------------------
Result PointerHandler::setScreenParams(MessageCallback messageCallback,
	int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
{
	mWidth = width;
	mHeight = height;
	mOffsetX = offsetX;
	mOffsetY = offsetY;
	mScaleX = scaleX;
	mScaleY = scaleY;

	return Result::OK;
}

// .NET available interface
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Create(MessageCallback messageCallback,
	Display* display, Window window, PointerCallback pointerCallback, void** handle) throw()
{
	PointerHandler* handler = new PointerHandler(display, window, pointerCallback);
	*handle = handler;

	PointerHandlerManager::pointerHandlers.insert(std::make_pair(window, handler));

	return handler->initialize(messageCallback);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Destroy(PointerHandler* handler) throw()
{
	PointerHandlerMapIterator it = PointerHandlerManager::pointerHandlers.find(handler->getWindow());
	if (it != PointerHandlerManager::pointerHandlers.end())
	{
		PointerHandlerManager::pointerHandlers.erase(it);
	}

	delete handler;
	return Result::OK;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_GetScreenResolution(
	PointerHandler* handler, MessageCallback messageCallback,
	int* width, int* height)
{
    return handler->getScreenResolution(messageCallback, width, height);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_SetScreenParams(
	PointerHandler* handler, MessageCallback messageCallback,
	int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
{
	return handler->setScreenParams(messageCallback, width, height, offsetX, offsetY, scaleX, scaleY);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_ProcessEventQueue(
	PointerHandler* handler, MessageCallback messageCallback, int frameCount)
{
	// We use the same architecture for pointer handlers and input. And as we
	// can't hook into the window procedures as we can on on Windows, for now
	// we do a process only once every frame.

	// An actual refactor of the C# side is required (creating a single MultiWindowStandardInput with multiple pointer handlers)
	// but that's for later.

	PointerHandlerManager::processEvents(frameCount);
	return Result::OK;
}
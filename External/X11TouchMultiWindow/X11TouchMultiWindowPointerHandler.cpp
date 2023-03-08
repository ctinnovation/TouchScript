/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/

#include <X11/extensions/XInput2.h>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowUtils.h"

// ----------------------------------------------------------------------------
PointerHandler::PointerHandler()
    : mDisplay(NULL)
	, mWindow(None)
	, mXInput2Opcode(0)
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
Result PointerHandler::initialize(MessageCallback messageCallback, Display* display, Window window)
{
    sendMessage(messageCallback, MessageType::INFO, "Initializing handler...");

	if (display == NULL)
	{
		sendMessage(messageCallback, MessageType::ERROR, "'display' is NULL");
		return Result::ERROR_NULL_POINTER;
	}

    if (window == None)
    {
        sendMessage(messageCallback, MessageType::ERROR, "'window' is None");
        return Result::ERROR_NULL_POINTER;
    }

	mDisplay = display;
    mWindow = window;

	// Request the opcode for XInput2
	int evt, err;
	if (!XQueryExtension(display, "XInputExtension", &mXInput2Opcode, &evt, &err))
	{
		sendMessage(messageCallback, MessageType::ERROR, "'XInput extension is not available");
		return Result::ERROR_UNSUPPORTED;
	}

	// Select the events we want to listen to
	XIEventMask mask = {
		.deviceid = XIAllDevices, // TODO Only touch devices
		.mask_len = XIMaskLen(XI_TouchEnd)
	};
	mask.mask = (unsigned char*)calloc(3, sizeof(char));

	XISetMask(mask.mask, XI_TouchBegin);
	XISetMask(mask.mask, XI_TouchUpdate);
	XISetMask(mask.mask, XI_TouchEnd);

	Status status = XISelectEvents(display, window, &mask, 1);
	if (status != Success)
	{
		sendMessage(messageCallback, MessageType::ERROR, "Failed to select touch events on window: " + std::to_string(status));

		free(mask.mask);
		return Result::ERROR_UNSUPPORTED;
	}

	free(mask.mask);
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
// ----------------------------------------------------------------------------
Result PointerHandler::processEventQueue(MessageCallback messageCallback, TouchEventCallback toucEventCallback)
{
	return Result::OK;
}

// .NET available interface
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Create(void** handle) throw()
{
	*handle = new PointerHandler();
	return Result::OK;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Destroy(PointerHandler* handler) throw()
{
	delete handler;
	return Result::OK;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Initialize(
	PointerHandler* handler, MessageCallback messageCallback,
	Display* display, Window window)
{
    return handler->initialize(messageCallback, display, window);
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
extern "C" EXPORT_API Result PointerHandler_ProcessEventQueu(
	PointerHandler* handler, MessageCallback messageCallback,
	TouchEventCallback touchEventCallback)
{
	return handler->processEventQueue(messageCallback, touchEventCallback);	
}
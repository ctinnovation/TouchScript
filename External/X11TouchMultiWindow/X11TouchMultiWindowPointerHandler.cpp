/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#include <cstring>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowUtils.h"

// ----------------------------------------------------------------------------
PointerHandler::PointerHandler(Display* display, Window window,
	MessageCallback messageCallback, PointerCallback pointerCallback)
    : mDisplay(display)
	, mWindow(window)
	, mMessageCallback(messageCallback)
	, mPointerCallback(pointerCallback)
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
Result PointerHandler::initialize()
{
    sendMessage(mMessageCallback, MessageType::INFO, "Initializing handler...");

	if (mDisplay == NULL)
	{
		sendMessage(mMessageCallback, MessageType::ERROR, "'display' is NULL");
		return Result::ERROR_NULL_POINTER;
	}

    if (mWindow == None)
    {
        sendMessage(mMessageCallback, MessageType::ERROR, "'window' is None");
        return Result::ERROR_NULL_POINTER;
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
		.deviceid = XIAllDevices, // TODO Only touch devices? Or XIAllMasterDevices?
		.mask_len = sizeof(mask),
		.mask = mask
	};

	Status status = XISelectEvents(mDisplay, mWindow, &eventMask, 1);
	free(eventMask.mask);
	
	if (status != Success)
	{
		sendMessage(mMessageCallback, MessageType::ERROR, "Failed to select pointer events on window: " + std::to_string(status));
		return Result::ERROR_UNSUPPORTED;
	}

    return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerHandler::getScreenResolution(int* width, int* height)
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
        sendMessage(mMessageCallback, MessageType::ERROR, "Failed to retrieve XWindowAttributes");
        return Result::ERROR_API;
    }
}
// ----------------------------------------------------------------------------
Result PointerHandler::setScreenParams(int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
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
void PointerHandler::processEvent(XIDeviceEvent* xiEvent)
{
	switch (xiEvent->evtype)
	{
		case XI_ButtonPress:
			break;
		case XI_ButtonRelease:
			break;
		case XI_TouchBegin:
			break;
		case XI_TouchUpdate:
			break;
		case XI_TouchEnd:
			break;
	}
}

// .NET available interface
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_GetScreenResolution(
	PointerHandler* handler, int* width, int* height)
{
    return handler->getScreenResolution(width, height);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_SetScreenParams(
	PointerHandler* handler, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
{
	return handler->setScreenParams(width, height, offsetX, offsetY, scaleX, scaleY);
}
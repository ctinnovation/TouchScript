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
    sendMessage(mMessageCallback, MessageType::MT_INFO, "Initializing handler...");

	if (mDisplay == NULL)
	{
		sendMessage(mMessageCallback, MessageType::MT_ERROR, "'display' is NULL");
		return Result::R_ERROR_NULL_POINTER;
	}

    if (mWindow == None)
    {
        sendMessage(mMessageCallback, MessageType::MT_ERROR, "'window' is None");
        return Result::R_ERROR_NULL_POINTER;
    }

	// Setup the event mask fore the events we want to listen to
	unsigned char mask[XIMaskLen(XI_LASTEVENT)];
	memset(mask, 0, sizeof(mask));
	// Mouse buttons
	XISetMask(mask, XI_ButtonPress);
	XISetMask(mask, XI_ButtonRelease);
	// Mouse motion
	XISetMask(mask, XI_Motion);
	// Touch
	XISetMask(mask, XI_TouchBegin);
	XISetMask(mask, XI_TouchUpdate);
	XISetMask(mask, XI_TouchEnd);

	XIEventMask eventMask = {
		.deviceid = XIAllMasterDevices, // TODO Only touch devices? Or XIAllDevices?
		.mask_len = sizeof(mask),
		.mask = mask
	};

	Status status = XISelectEvents(mDisplay, mWindow, &eventMask, 1);
	if (status != Success)
	{
		sendMessage(mMessageCallback, MessageType::MT_ERROR, "Failed to select pointer events on window: " + std::to_string(status));
		return Result::R_ERROR_UNSUPPORTED;
	}

	// Propagate request to X server
	XFlush(mDisplay);

	sendMessage(mMessageCallback, MessageType::MT_INFO, "Handler initialized...");

    return Result::R_OK;
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
        return Result::R_OK;
    }
    else
    {
        sendMessage(mMessageCallback, MessageType::MT_ERROR, "Failed to retrieve XWindowAttributes");
        return Result::R_ERROR_API;
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

	return Result::R_OK;
}
// ----------------------------------------------------------------------------
void PointerHandler::processEvent(XIDeviceEvent* xiEvent)
{
	int pointerId = 0;
	PointerType pointerType = PointerType::PT_NONE;
	PointerEvent pointerEvent;
	PointerData pointerData;

	switch (xiEvent->evtype)
	{
		case XI_ButtonPress:
			{
				int button = xiEvent->detail;
				if (button < 1 || button > 5)
				{
					return;
				}

				pointerType = PointerType::PT_MOUSE;
				pointerEvent = PointerEvent::PE_DOWN;

				pointerData.flags = (PointerFlags)(0x10 << (button - 1));
				pointerData.changedButtons = (PointerButtonChangeType)((button * 2) - 1);
			}
			break;
		case XI_ButtonRelease:
			{
				int button = xiEvent->detail;
				if (button < 1 || button > 5)
				{
					return;
				}

				pointerType = PointerType::PT_MOUSE;
				pointerEvent = PointerEvent::PE_UP;

				pointerData.flags = (PointerFlags)(0x10 << (button - 1));
				pointerData.changedButtons = (PointerButtonChangeType)(button * 2);
			}
			break;
		case XI_Motion:
			{
				pointerType = PointerType::PT_MOUSE;
				pointerEvent = PointerEvent::PE_UPDATE;
				pointerData.changedButtons = PointerButtonChangeType::PBCT_NONE;
			}
			break;
		case XI_TouchBegin:
			{
				pointerId = xiEvent->detail;
				pointerType = PointerType::PT_TOUCH;
				pointerEvent = PointerEvent::PE_DOWN;
			}
			break;
		case XI_TouchUpdate:
			{
				pointerId = xiEvent->detail;
				pointerType = PointerType::PT_TOUCH;
				pointerEvent = PointerEvent::PE_UPDATE;
			}
			break;
		case XI_TouchEnd:
			{
				pointerId = xiEvent->detail;
				pointerType = PointerType::PT_TOUCH;
				pointerEvent = PointerEvent::PE_UP;
			}
			break;
	}

	if (pointerType == PointerType::PT_NONE)
	{
		return;
	}
 
	Vector2 position = Vector2(
		((float)xiEvent->event_x - mOffsetX) * mScaleX,
		mHeight - ((float)xiEvent->event_y - mOffsetY) * mScaleY);

	mPointerCallback(pointerId, pointerEvent, pointerType, position, pointerData);
}
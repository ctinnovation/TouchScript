/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#include <cstring>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowUtils.h"

// ----------------------------------------------------------------------------
PointerHandler::PointerHandler(Display* display, int targetDisplay, Window window,
	MessageCallback messageCallback, PointerCallback pointerCallback)
    : mDisplay(display)
	, mTargetDisplay(targetDisplay)
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
Result PointerHandler::initialize(std::vector<int> deviceIds)
{
    sendMessage(mMessageCallback, MT_INFO, "Initializing handler for display " + 
		std::to_string(mTargetDisplay) + " with window " + std::to_string(mWindow) + "...");

	if (mDisplay == NULL)
	{
		sendMessage(mMessageCallback, MT_ERROR, "'display' is NULL");
		return R_ERROR_NULL_POINTER;
	}

    if (mWindow == None)
    {
        sendMessage(mMessageCallback, MT_ERROR, "'window' is None");
        return R_ERROR_NULL_POINTER;
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

	Status status = Success;
	for (std::vector<int>::const_iterator it = deviceIds.begin(); it != deviceIds.end(); ++it)
	{
		XIEventMask eventMask = {
			.deviceid = *it,
			.mask_len = sizeof(mask),
			.mask = mask
		};

		Status s = XISelectEvents(mDisplay, mWindow, &eventMask, 1);
		if (s != Success)
		{
			sendMessage(mMessageCallback, MT_ERROR, "Failed to select events for display " +
				std::to_string(mTargetDisplay) + ": " + std::to_string(status));
			status = s;
		}
	}

	if (status != Success)
	{
		sendMessage(mMessageCallback, MT_ERROR, "Failed to select events for display " +
			std::to_string(mTargetDisplay) + ": " + std::to_string(status));
		return R_ERROR_UNSUPPORTED;
	}

	// Propagate requests to X server
	XFlush(mDisplay);

	sendMessage(mMessageCallback, MT_INFO, "Handler for display " + std::to_string(mTargetDisplay) + " initialized");

    return R_OK;
}
// ----------------------------------------------------------------------------
Result PointerHandler::getScreenParams(int*x, int*y, int* width, int* height,
	int* screenWidth, int* screenHeight)
{
	sendMessage(mMessageCallback, MT_INFO, "Requesting screen resolution of window " +
		std::to_string(mWindow));

    // Get the screen for the window
    XWindowAttributes attributes;
    if (XGetWindowAttributes(mDisplay, mWindow, &attributes) != 0)
    {
		*x = attributes.x;
		*y = attributes.y;
        *width = attributes.width;
        *height = attributes.height;
        return R_OK;
    }
    else
    {
        sendMessage(mMessageCallback, MT_ERROR, "Failed to retrieve XWindowAttributes");
        return R_ERROR_API;
    }
}
// ----------------------------------------------------------------------------
Result PointerHandler::setScreenParams(int width, int height, float offsetX, float offsetY,
	float scaleX, float scaleY)
{
	mWidth = width;
	mHeight = height;
	mOffsetX = offsetX;
	mOffsetY = offsetY;
	mScaleX = scaleX;
	mScaleY = scaleY;

	return R_OK;
}
// ----------------------------------------------------------------------------
void PointerHandler::processEvent(XIDeviceEvent* xiEvent)
{
	int pointerId = 0;
	PointerType pointerType;
	PointerEvent pointerEvent;
	PointerData pointerData;

	sendMessage(mMessageCallback, MT_DEBUG, "Processing input for display " + std::to_string(mTargetDisplay));

	switch (xiEvent->evtype)
	{
		case XI_ButtonPress:
			{
				int button = xiEvent->detail;
				if (button < 1 || button > 5)
				{
					return;
				}

				pointerType = PT_MOUSE;
				pointerEvent = PE_DOWN;

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

				pointerType = PT_MOUSE;
				pointerEvent = PE_UP;

				pointerData.flags = (PointerFlags)(0x10 << (button - 1));
				pointerData.changedButtons = (PointerButtonChangeType)(button * 2);
			}
			break;
		case XI_Motion:
			{
				pointerType = PT_MOUSE;
				pointerEvent = PE_UPDATE;
				pointerData.changedButtons = PBCT_NONE;
			}
			break;
		case XI_TouchBegin:
			{
				pointerId = xiEvent->detail;
				pointerType = PT_TOUCH;
				pointerEvent = PE_DOWN;
			}
			break;
		case XI_TouchUpdate:
			{
				pointerId = xiEvent->detail;
				pointerType = PT_TOUCH;
				pointerEvent = PE_UPDATE;
			}
			break;
		case XI_TouchEnd:
			{
				pointerId = xiEvent->detail;
				pointerType = PT_TOUCH;
				pointerEvent = PE_UP;
			}
			break;
		default:
			return;
	}
 
	Vector2 position = Vector2(
		((float)xiEvent->event_x - mOffsetX) * mScaleX,
		mHeight - ((float)xiEvent->event_y - mOffsetY) * mScaleY);

	mPointerCallback(pointerId, pointerEvent, pointerType, position, pointerData);
}
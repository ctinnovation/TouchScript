/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowUtils.h"

// ----------------------------------------------------------------------------
PointerHandler::PointerHandler()
    : mDisplay(NULL)
	, mWindow(None)
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

    return Result::OK;
}
// ----------------------------------------------------------------------------
Result PointerHandler::getScreenResolution(MessageCallback messageCallback, int* width, int* height)
{
    // Get the screen for the window
    XWindowAttributes attributes; 
    if (XGetWindowAttributes(mDisplay, mWindow, &attributes) != 0)
    {
        *width = XWidthOfScreen(attributes.Screen);
        *height = XHeightOfScreen(attributes.Screen);
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
/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#pragma once

#include <X11/Xlib.h>
#include <X11/extensions/XInput2.h>

#include "X11TouchMultiWindowCommon.h"

class EXPORT_API PointerHandler
{
private:
    Display* mDisplay;
    Window mWindow;
    MessageCallback mMessageCallback;
    PointerCallback mPointerCallback;

    int mWidth;
	int mHeight;

	float mOffsetX;
	float mOffsetY;

	float mScaleX;
	float mScaleY;
public:
    PointerHandler(Display* display, Window window, MessageCallback messageCallback, PointerCallback pointerCallback);
    ~PointerHandler();

    Window getWindow() const { return mWindow; }

    Result initialize();
    Result getScreenResolution(int* width, int* height);
    Result setScreenParams(int width, int height, float offsetX, float offsetY, float scaleX, float scaleY);

    void processEvent(XIDeviceEvent* xiEvent);
};
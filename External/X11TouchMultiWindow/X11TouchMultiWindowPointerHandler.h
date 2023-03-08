/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/

#pragma once

#include <X11/Xlib.h>

#include "X11TouchMultiWindowCommon.h"



class EXPORT_API PointerHandler
{
private:
    Display* mDisplay;
    Window mWindow;
    PointerCallback mPointerCallback;
    int mXInputOpcode;

    int mWidth;
	int mHeight;

	float mOffsetX;
	float mOffsetY;

	float mScaleX;
	float mScaleY;
public:
    PointerHandler(Display* display, Window window, PointerCallback pointerCallback);
    ~PointerHandler();

    Window getWindow() const { return mWindow; }

    Result initialize(MessageCallback messageCallback);
    Result getScreenResolution(MessageCallback messageCallback, int* width, int* height);
    Result setScreenParams(MessageCallback messageCallback, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY);
    Result processEvents(MessageCallback messageCallback, int frameCount);
};
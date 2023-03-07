/*
@author Jorrit de Vries (jorrit@jorritdevries.com)
*/

#pragma once

#include <X11/Xlib.h>

#include "X11TouchMultiWindowCommon.h"

class EXPORT_API PointerHandler
{
private:
    Display* mDisplay;
    Window mWindow;

    int mWidth;
	int mHeight;

	float mOffsetX;
	float mOffsetY;

	float mScaleX;
	float mScaleY;
public:
    PointerHandler();
    ~PointerHandler();

    Result initialize(MessageCallback messageCallback, Display* display, Window window);
    Result getScreenResolution(MessageCallback messageCallback, int* width, int* height);
    Result setScreenParams(MessageCallback messageCallback, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY);
};
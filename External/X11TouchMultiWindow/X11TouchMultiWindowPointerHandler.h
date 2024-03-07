/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#pragma once

#include <vector>
#include <X11/Xlib.h>
#include <X11/extensions/XInput2.h>

#include "X11TouchMultiWindowCommon.h"

class EXPORT_API PointerHandler
{
private:
	Display* mDisplay;
	int mTargetDisplay;
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
	PointerHandler(Display* display, int targetDisplay, Window window,
		MessageCallback messageCallback, PointerCallback pointerCallback);
	~PointerHandler();

	Result initialize(std::vector<int> deviceIds);

	Window getWindow() const { return mWindow; }
	int getTargetDisplay() const { return mTargetDisplay; }
	Result setTargetDisplay(int value) { mTargetDisplay = value; return R_OK; }

	Result getScreenParams(int* positionX, int* positionY, int* width, int* height,
		int* screenWidth, int* screenHeight);
	Result setScreenParams(int width, int height, float offsetX, float offsetY,
		float scaleX, float scaleY);

	void processEvent(XIDeviceEvent* xiEvent);
};
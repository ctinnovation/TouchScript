/*
* @author Jorrit de Vries (jorrit@ijsfontein.nl)
*/

#pragma once

#include <string>

#include "WindowsTouchMultiWindow.h"
#include "WindowsTouchMultiWindowCommon.h"

class EXPORT_API PointerHandler
{
private:
	int mTargetDisplay;
	TOUCH_API mApi;
	HWND mHWnd;
	HINSTANCE mHInstance;
	LONG_PTR mPreviousWndProc;
	GET_POINTER_INFO mGetPointerInfo;
	GET_POINTER_TOUCH_INFO mGetPointerTouchInfo;
	GET_POINTER_PEN_INFO mGetPointerPenInfo;
	PointerCallback mPointerCallback;

	int mWidth;
	int mHeight;

	float mOffsetX;
	float mOffsetY;

	float mScaleX;
	float mScaleY;
public:
	/**	*/
	PointerHandler();
	/**	*/
	~PointerHandler();

	/**	*/
	Result initialize(MessageCallback messageCallback, int targetDisplay, TOUCH_API api, HWND hWnd, PointerCallback pointerCallback);
	
	int getTargetDisplay() const { return mTargetDisplay; }
    Result setTargetDisplay(int value) { mTargetDisplay = value; return R_OK; }
	/**	*/
	Result setScreenParams(MessageCallback messageCallback, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY);
private:
	/**	*/
	void sendMessage(MessageCallback messageCallback, MessageType messageType, const std::string& message);
	/**	*/
	void decodeWin8Touches(UINT msg, WPARAM wParam, LPARAM lParam);
	/**	*/
	void decodeWin7Touches(UINT msg, WPARAM wParam, LPARAM lParam);

	/**	*/
	static LRESULT CALLBACK wndProc8(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
	/**	*/
	static LRESULT CALLBACK wndProc7(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
};

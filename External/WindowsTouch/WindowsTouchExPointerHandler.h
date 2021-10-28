/*
* @author Jorrit de Vries (jorrit@jorritdevries.com)
*/

#pragma once

#include <string>

#include "WindowsTouchEx.h"
#include "WindowsTouchExCommon.h"

class EXPORT_API PointerHandler
{
private:
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
	Result initialize(MessageCallback messageCallback, TOUCH_API api, HWND hWnd, PointerCallback pointerCallback);
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
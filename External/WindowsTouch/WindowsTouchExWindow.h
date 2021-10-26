#pragma once
#include "WindowsTouchEx.h"

namespace WindowsTouchEx
{
	class EXPORT_API Window
	{
	private:
		HWND mHWnd;

		TOUCH_API mApi;
		LogFuncPtr mLog;
		PointerDelegatePtr mDelegate;

		LONG_PTR mPreviousWndProc;

		int mScreenWidth;
		int mScreenHeight;

		float mOffsetX;
		float mOffsetY;

		float mScaleX;
		float mScaleY;
	public:
		Window(HWND hWnd, TOUCH_API api, LogFuncPtr log, PointerDelegatePtr delegate);
		~Window();

		void setScreenParams(int width, int height, float offsetX, float offsetY, float scaleX, float scaleY);
	private:
		void log(const wchar_t* str);

		LRESULT CALLBACK wndProc8(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
		LRESULT CALLBACK wndProc7(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);

		void decodeWin8Touches(UINT msg, WPARAM wParam, LPARAM lParam);
		void decodeWin7Touches(UINT msg, WPARAM wParam, LPARAM lParam);
	};
}
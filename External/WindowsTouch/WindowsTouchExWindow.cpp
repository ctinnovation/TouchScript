#include "WindowsTouchExWindow.h"

namespace WindowsTouchEx
{
	// ------------------------------------------------------------------------
	Window::Window(HWND hWnd, TOUCH_API api, LogFuncPtr log, PointerDelegatePtr delegate)
		: mHWnd(hWnd)
		, mApi(api)
		, mLog(log)
		, mDelegate(delegate)
		, mPreviousWndProc(0)
		, mScreenWidth(0)
		, mScreenHeight(0)
		, mOffsetX(0.0f)
		, mOffsetY(0.0f)
		, mScaleX(0.0f)
		, mScaleY(0.0f)
	{

	}

	// ------------------------------------------------------------------------
	Window::~Window()
	{
		if (mPreviousWndProc)
		{
			SetWindowLongPtr(mHWnd, GWLP_WNDPROC, mPreviousWndProc);
			mPreviousWndProc = 0;

			if (mApi == WIN7)
			{
				UnregisterTouchWindow(mHWnd);
			}
		}
	}

	// ------------------------------------------------------------------------
	void Window::setScreenParams(int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
	{
		mScreenWidth = width;
		mScreenHeight - height;
		mOffsetX = offsetX;
		mOffsetY = offsetY;
		mScaleX = scaleX;
		mScaleY = scaleY;
	}

	// ------------------------------------------------------------------------
	void Window::log(const wchar_t* str)
	{
#if _DEBUG
		BSTR bstr = SysAllocString(str);
		mLog(bstr);
		SysFreeString(bstr);
#endif
	}

	// ------------------------------------------------------------------------
	LRESULT CALLBACK Window::wndProc8(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
	{
		switch (msg)
		{
		case WM_TOUCH:
			CloseTouchInputHandle((HTOUCHINPUT)lParam);
			break;
		case WM_POINTERENTER:
		case WM_POINTERLEAVE:
		case WM_POINTERDOWN:
		case WM_POINTERUP:
		case WM_POINTERUPDATE:
		case WM_POINTERCAPTURECHANGED:
			decodeWin8Touches(msg, wParam, lParam);
			break;
		default:
			return CallWindowProc((WNDPROC)mPreviousWndProc, hwnd, msg, wParam, lParam);
		}
		return 0;
	}

	// ------------------------------------------------------------------------
	LRESULT CALLBACK Window::wndProc7(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
	{
		switch (msg)
		{
		case WM_TOUCH:
			decodeWin7Touches(msg, wParam, lParam);
			break;
		default:
			return CallWindowProc((WNDPROC)mPreviousWndProc, hwnd, msg, wParam, lParam);
		}
		return 0;
	}

	// ------------------------------------------------------------------------
	void Window::decodeWin8Touches(UINT msg, WPARAM wParam, LPARAM lParam)
	{
		int pointerId = GET_POINTERID_WPARAM(wParam);

		POINTER_INFO pointerInfo;
		if (!GetPointerInfo(pointerId, &pointerInfo)) return;

		POINT p;
		p.x = pointerInfo.ptPixelLocation.x;
		p.y = pointerInfo.ptPixelLocation.y;
		ScreenToClient(mHWnd, &p);

		Vector2 position = Vector2(((float)p.x - mOffsetX) * mScaleX, mScreenHeight - ((float)p.y - mOffsetY) * mScaleY);
		PointerData data{};
		data.pointerFlags = pointerInfo.pointerFlags;
		data.changedButtons = pointerInfo.ButtonChangeType;

		if ((pointerInfo.pointerFlags & POINTER_FLAG_CANCELED) != 0
			|| msg == WM_POINTERCAPTURECHANGED) msg = POINTER_CANCELLED;

		switch (pointerInfo.pointerType)
		{
		case PT_MOUSE:
			break;
		case PT_TOUCH:
			POINTER_TOUCH_INFO touchInfo;
			GetPointerTouchInfo(pointerId, &touchInfo);
			data.flags = touchInfo.touchFlags;
			data.mask = touchInfo.touchMask;
			data.rotation = touchInfo.orientation;
			data.pressure = touchInfo.pressure;
			break;
		case PT_PEN:
			POINTER_PEN_INFO penInfo;
			GetPointerPenInfo(pointerId, &penInfo);
			data.flags = penInfo.penFlags;
			data.mask = penInfo.penMask;
			data.rotation = penInfo.rotation;
			data.pressure = penInfo.pressure;
			data.tiltX = penInfo.tiltX;
			data.tiltY = penInfo.tiltY;
			break;
		}

		mDelegate(pointerId, msg, pointerInfo.pointerType, position, data);
	}

	// ------------------------------------------------------------------------
	void Window::decodeWin7Touches(UINT msg, WPARAM wParam, LPARAM lParam)
	{
		UINT cInputs = LOWORD(wParam);
		PTOUCHINPUT pInputs = new TOUCHINPUT[cInputs];

		if (!pInputs) return;
		if (!GetTouchInputInfo((HTOUCHINPUT)lParam, cInputs, pInputs, sizeof(TOUCHINPUT))) return;

		for (UINT i = 0; i < cInputs; i++)
		{
			TOUCHINPUT touch = pInputs[i];

			POINT p;
			p.x = touch.x / 100;
			p.y = touch.y / 100;
			ScreenToClient(mHWnd, &p);

			Vector2 position = Vector2(((float)p.x - mOffsetX) * mScaleX, mScreenHeight - ((float)p.y - mOffsetY) * mScaleY);
			PointerData data{};

			if ((touch.dwFlags & TOUCHEVENTF_DOWN) != 0)
			{
				msg = WM_POINTERDOWN;
				data.changedButtons = POINTER_CHANGE_FIRSTBUTTON_DOWN;
			}
			else if ((touch.dwFlags & TOUCHEVENTF_UP) != 0)
			{
				msg = WM_POINTERLEAVE;
				data.changedButtons = POINTER_CHANGE_FIRSTBUTTON_UP;
			}
			else if ((touch.dwFlags & TOUCHEVENTF_MOVE) != 0)
			{
				msg = WM_POINTERUPDATE;
			}

			mDelegate(touch.dwID, msg, PT_TOUCH, position, data);
		}

		CloseTouchInputHandle((HTOUCHINPUT)lParam);
		delete[] pInputs;
	}
}
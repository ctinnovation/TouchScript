/*
* @author Jorrit de Vries (jorrit@jorritdevries.com)
*/

#include "WindowsTouchExPointerHandler.h"

const wchar_t* instancePropName = L"__PointerHandler_Prop_Instance__";

// ----------------------------------------------------------------------------
PointerHandler::PointerHandler()
	: mApi(WIN8)
	, mHWnd(NULL)
	, mPreviousWndProc(NULL)
	, mGetPointerInfo(NULL)
	, mGetPointerTouchInfo(NULL)
	, mGetPointerPenInfo(NULL)
	, mPointerCallback(NULL)
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
	if (mHWnd)
	{
		RemoveProp(mHWnd, instancePropName);
	}

	if (mPreviousWndProc)
	{
		SetWindowLongPtr(mHWnd, GWLP_WNDPROC, mPreviousWndProc);
		mPreviousWndProc = NULL;

		if (mApi == WIN7)
		{
			UnregisterTouchWindow(mHWnd);
		}
	}
}

// ----------------------------------------------------------------------------
Result PointerHandler::initialize(MessageCallback messageCallback,
	TOUCH_API api, HWND hWnd, PointerCallback pointerCallback)
{
	sendMessage(messageCallback, MT_INFO, "Initializing handler...");

	if (hWnd == NULL)
	{
		sendMessage(messageCallback, MT_ERROR, "hWnd is NULL");
		return R_ERROR_NULL_POINTER;
	}

	if (pointerCallback == NULL)
	{
		sendMessage(messageCallback, MT_ERROR, "pointerCallback is NULL");
		return R_ERROR_NULL_POINTER;
	}

	mApi = api;
	mHWnd = hWnd;
	mPointerCallback = pointerCallback;

	if (api == WIN8)
	{
		HINSTANCE hInstance = LoadLibrary(TEXT("user32.dll"));
		if (hInstance == NULL)
		{
			sendMessage(messageCallback, MT_ERROR, "Failed to load user32.dll.");
			return R_ERROR_API;
		}

		mGetPointerInfo = (GET_POINTER_INFO)GetProcAddress(hInstance, "GetPointerInfo");
		mGetPointerTouchInfo = (GET_POINTER_TOUCH_INFO)GetProcAddress(hInstance, "GetPointerTouchInfo");
		mGetPointerPenInfo = (GET_POINTER_PEN_INFO)GetProcAddress(hInstance, "GetPointerPenInfo");

		SetProp(mHWnd, instancePropName, this);
		mPreviousWndProc = SetWindowLongPtr(mHWnd, GWLP_WNDPROC, (LONG_PTR)wndProc8);

		sendMessage(messageCallback, MT_INFO, "Handler has been initialized for WIN8+.");
	}
	else
	{
		RegisterTouchWindow(mHWnd, 0);

		mPreviousWndProc = SetWindowLongPtr(mHWnd, GWLP_WNDPROC, (LONG_PTR)wndProc7);

		sendMessage(messageCallback, MT_INFO, "Handler has been initialized for WIN7.");
	}

	return R_OK;
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

	return R_OK;
}

// ----------------------------------------------------------------------------
void PointerHandler::sendMessage(MessageCallback messageCallback,
	MessageType messageType, const std::string& message)
{
	if (messageCallback)
	{
		// Allocate char array
		char* cstr = new char[message.length() + 1];
		strcpy_s(cstr, message.length() + 1, message.c_str());

		// Dispatch to callback
		messageCallback((int)messageType, cstr);

		// Unalloc char array
		delete[] cstr;
	}
}

// ----------------------------------------------------------------------------
void PointerHandler::decodeWin8Touches(UINT msg, WPARAM wParam, LPARAM lParam)
{
	int pointerId = GET_POINTERID_WPARAM(wParam);

	POINTER_INFO pointerInfo;
	if (!mGetPointerInfo(pointerId, &pointerInfo)) return;

	POINT p;
	p.x = pointerInfo.ptPixelLocation.x;
	p.y = pointerInfo.ptPixelLocation.y;
	ScreenToClient(mHWnd, &p);

	Vector2 position = Vector2(((float)p.x - mOffsetX) * mScaleX, mHeight - ((float)p.y - mOffsetY) * mScaleY);
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
		mGetPointerTouchInfo(pointerId, &touchInfo);
		data.flags = touchInfo.touchFlags;
		data.mask = touchInfo.touchMask;
		data.rotation = touchInfo.orientation;
		data.pressure = touchInfo.pressure;
		break;
	case PT_PEN:
		POINTER_PEN_INFO penInfo;
		mGetPointerPenInfo(pointerId, &penInfo);
		data.flags = penInfo.penFlags;
		data.mask = penInfo.penMask;
		data.rotation = penInfo.rotation;
		data.pressure = penInfo.pressure;
		data.tiltX = penInfo.tiltX;
		data.tiltY = penInfo.tiltY;
		break;
	}

	mPointerCallback(pointerId, msg, pointerInfo.pointerType, position, data);
}

// ----------------------------------------------------------------------------
void PointerHandler::decodeWin7Touches(UINT msg, WPARAM wParam, LPARAM lParam)
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

		Vector2 position = Vector2(((float)p.x - mOffsetX) * mScaleX, mHeight - ((float)p.y - mOffsetY) * mScaleY);
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

		mPointerCallback(touch.dwID, msg, PT_TOUCH, position, data);
	}

	CloseTouchInputHandle((HTOUCHINPUT)lParam);
	delete[] pInputs;
}

// ----------------------------------------------------------------------------
LRESULT CALLBACK PointerHandler::wndProc8(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	PointerHandler* handler = reinterpret_cast<PointerHandler*>(GetProp(hWnd, instancePropName));

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
		handler->decodeWin8Touches(msg, wParam, lParam);
		break;
	default:
		return CallWindowProc((WNDPROC)handler->mPreviousWndProc, hWnd, msg, wParam, lParam);
	}
	return 0;
}

// ----------------------------------------------------------------------------
LRESULT CALLBACK PointerHandler::wndProc7(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	PointerHandler* handler = reinterpret_cast<PointerHandler*>(GetProp(hWnd, instancePropName));

	switch (msg)
	{
	case WM_TOUCH:
		handler->decodeWin7Touches(msg, wParam, lParam);
		break;
	default:
		return CallWindowProc((WNDPROC)handler->mPreviousWndProc, hWnd, msg, wParam, lParam);
	}
	return 0;
}

// .NET available interface
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Create(void** handle) throw()
{
	*handle = new PointerHandler();
	return Result::R_OK;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Destroy(PointerHandler* handler) throw()
{
	delete handler;
	return Result::R_OK;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Initialize(
	PointerHandler * handler, MessageCallback messageCallback,
	TOUCH_API api, HWND hWnd, PointerCallback pointerCallback)
{
	return handler->initialize(messageCallback, api, hWnd, pointerCallback);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_SetScreenParams(
	PointerHandler * handler, MessageCallback messageCallback,
	int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
{
	return handler->setScreenParams(messageCallback, width, height, offsetX, offsetY, scaleX, scaleY);
}
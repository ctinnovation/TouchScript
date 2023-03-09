#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowPointerHandlerSystem.h"

PointerHandlerSystem* system;

// .NET available interface
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandlerSystem_ProcessEventQueue()
{
    if (system == nullptr)
    {
        return Result::ERROR_NULL_POINTER;
    }

    return system->processEventQueue();
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandlerSystem_GetWindowsOfProcess(MessageCallback messageCallback,
    int processID, Window** windows, uint* numWindows)
{
    if (system == nullptr)
    {
        system = new PointerHandlerSystem(messageCallback);
        Result result = system->initialize();
        if (result != Result::OK)
        {
            return result;
        }
    }

    return system->getWindowsOfProcess(processID, windows, numWindows);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandlerSystem_FreeWindowsOfProcess(Window* windows)
{
    if (system == nullptr)
    {
        return Result::ERROR_NULL_POINTER;
    }
    
    return system->freeWindowsOfProcess(windows);
}

// ----------------------------------------------------------------------------
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Create(Window window,
    PointerCallback pointerCallback, MessageCallback messageCallback, void** handle) throw()
{
    if (system == nullptr)
    {
        system = new PointerHandlerSystem(messageCallback);
        Result result = system->initialize();
        if (result != Result::OK)
        {
            return result;
        }
    }

	return system->createHandler(window, pointerCallback, handle);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Destroy(PointerHandler* handler) throw()
{
    if (system == nullptr)
    {
        return Result::ERROR_NULL_POINTER;
    }

	return system->destroyHandler(handler);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_GetScreenResolution(
	PointerHandler* handler, int* width, int* height)
{
    return handler->getScreenResolution(width, height);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_SetScreenParams(
	PointerHandler* handler, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
{
	return handler->setScreenParams(width, height, offsetX, offsetY, scaleX, scaleY);
}
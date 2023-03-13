/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/

#include <cassert>

#include "X11TouchMultiWindowPointerHandler.h"
#include "X11TouchMultiWindowPointerHandlerSystem.h"

// .NET available interface
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandlerSystem_Create(MessageCallback messageCallback, void** handle) throw()
{
    PointerHandlerSystem* system = PointerHandlerSystem::getInstance();
    if (system != nullptr)
    {
        *handle = system;
        return R_OK;
    }

    system = new PointerHandlerSystem(messageCallback);
    Result result = system->initialize();
    if (result == R_OK)
    {
        *handle = system;
    }
    else
    {
        delete system;
    }
    
    return result;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandlerSystem_Destroy(PointerHandlerSystem* system)
{
    if (system != nullptr)
    {
        delete system;
        system = 0;
    }

    return R_OK;
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandlerSystem_ProcessEventQueue(PointerHandlerSystem* system)
{
    if (system == nullptr)
    {
        return R_ERROR_NULL_POINTER;
    }

    return system->processEventQueue();
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandlerSystem_GetWindowsOfProcess(PointerHandlerSystem* system,
    int processID, Window** windows, uint* numWindows)
{
    if (system == nullptr)
    {
        return R_ERROR_NULL_POINTER;
    }

    return system->getWindowsOfProcess(processID, windows, numWindows);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandlerSystem_FreeWindowsOfProcess(PointerHandlerSystem* system,
    Window* windows)
{
    if (system == nullptr)
    {
        return R_ERROR_NULL_POINTER;
    }
    
    return system->freeWindowsOfProcess(windows);
}

// ----------------------------------------------------------------------------
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Create(Window window,
    PointerCallback pointerCallback, void** handle) throw()
{
    PointerHandlerSystem* system = PointerHandlerSystem::getInstance();
    if (system == nullptr)
    {
        return R_ERROR_NULL_POINTER;
    }

	return system->createHandler(window, pointerCallback, handle);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_Destroy(PointerHandler* handler) throw()
{
    PointerHandlerSystem* system = PointerHandlerSystem::getInstance();
    if (system == nullptr)
    {
        return R_ERROR_NULL_POINTER;
    }

	return system->destroyHandler(handler);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_GetScreenParams(
	PointerHandler* handler, int* x, int* y, int* width, int* height, int* screenWidth, int* screenHeight)
{
    return handler->getScreenParams(x, y, width, height, screenWidth, screenHeight);
}
// ----------------------------------------------------------------------------
extern "C" EXPORT_API Result PointerHandler_SetScreenParams(
	PointerHandler* handler, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
{
	return handler->setScreenParams(width, height, offsetX, offsetY, scaleX, scaleY);
}
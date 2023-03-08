#include "X11TouchMultiWindowPointerHandlerManager.h"

int PointerHandlerManager::mLastFrameCount = -1;

// ----------------------------------------------------------------------------
void PointerHandlerManager::processEvents(MessageCallback messageCallback,
    Display* display, int opcode, int frameCount)
{
    if (mLastFrameCount == frameCount)
    {
        // Already processed the event queue this frame
        return;
    }

    mLastFrameCount = frameCount;

    XEvent e;
    while (XEventsQueued(display, QueuedAlready))
    {
        NextEvent(display, &e);
        if (e.type != GenericEvent || e.xcookie.extension != opcode)
        {
            // Received a non xinput event
            continue;
        }

        XIDeviceEvent* xiEvent = (XIDeviceEvent*)e.xcookie.data;
        Window window = xiEvent->event;


        switch (xiEvent->evtype)
        {
            case XI_ButtonPress:
                break;
            case XI_ButtonRelease:
                break;
            case XI_TouchBegin:
                break;
            case XI_TouchUpdate:
                break;
            case XI_TouchEnd:
                break;
        }
    }
}
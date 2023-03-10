#include <cstring>
#include <iostream>
#include <string>
#include <X11/Xlib.h>
#include <X11/extensions/XInput2.h>

bool processEvent(XIDeviceEvent* xiEvent)
{
    switch (xiEvent->evtype)
	{
		case XI_ButtonPress:
			break;
		case XI_ButtonRelease:
			break;
		case XI_Motion:
			break;
		case XI_TouchBegin:
			break;
		case XI_TouchUpdate:
			break;
		case XI_TouchEnd:
			break;
	}
    
    return false;
}

int main()
{
    Display* display = XOpenDisplay(NULL);

    int opcode, event, error;
    if (!XQueryExtension(display, "XInputExtension", &opcode, &event, &error))
    {
        fprintf(stderr, "Failed to get the XInput extension.");

        XCloseDisplay(display);
        return -1;
    }

    int major = 2, minor = 3;
    if (XIQueryVersion(display, &major, &minor) == BadRequest)
    {
        std::string msg = "Unsupported XInput extension version: expected 2.3+, actual " +
            std::to_string(major) + "." + std::to_string(minor);
        fprintf(stderr, msg.c_str());

        XCloseDisplay(display);
        return -1;
    }

    Window window = XCreateSimpleWindow(display, RootWindow(display, DefaultScreen(display)), 20, 20, 640, 480, 0, BlackPixel(display, DefaultScreen(display)), WhitePixel(display, DefaultScreen(display)));

    unsigned char mask[XIMaskLen(XI_LASTEVENT)];
	memset(mask, 0, sizeof(mask));
	XISetMask(mask, XI_ButtonPress);
	XISetMask(mask, XI_ButtonRelease);
	XISetMask(mask, XI_Motion);
	XISetMask(mask, XI_TouchBegin);
	XISetMask(mask, XI_TouchUpdate);
	XISetMask(mask, XI_TouchEnd);

	XIEventMask eventMask = {
		.deviceid = XIAllDevices, // TODO Only touch devices? Or XIAllMasterDevices?
		.mask_len = sizeof(mask),
		.mask = mask
	};

	Status status = XISelectEvents(display, window, &eventMask, 1);

    if (status != Success)
	{
        std::string msg = "Failed to select pointer events on window: " + std::to_string(status);
		fprintf(stderr, msg.c_str());

        XCloseDisplay(display);
        return -1;
    }

    XMapWindow(display, window);

    bool shutdownRequested = false;
    while (!shutdownRequested)
    {
        XEvent xEvent;
        while (XEventsQueued(display, QueuedAlready))
        {
            XNextEvent(display, &xEvent);
            if (xEvent.type != GenericEvent || xEvent.xcookie.extension != opcode)
            {
                std::string msg = "Received event of type " + std::to_string(xEvent.type) + " for window " + std::to_string(window);
                // Received a non xinput event
                printf(msg.c_str());
                continue;
            }

            //XGetEventData(mDisplay, &e.xcookie);
            XIDeviceEvent* xiEvent = (XIDeviceEvent*)xEvent.xcookie.data;
            
            Window w = xiEvent->event;
            if (w != window)
            {
                printf("Received event from unknown window");
                continue;
            }

            if (xEvent.xany.window != window)
            {
                printf("Received event from unknown xany.window");
                continue;
            }

            shutdownRequested = processEvent(xiEvent);

            //XFreeEventData(mDisplay, &e.xcookie);
        }
    }

    XUnmapWindow(display, window);
    XCloseDisplay(display);
}
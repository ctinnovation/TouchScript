#include <cstring>
#include <iostream>
#include <string>
#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <X11/extensions/XInput2.h>

bool processEvent(XIDeviceEvent* xiEvent)
{
    switch (xiEvent->evtype)
	{
		case XI_ButtonPress:
            std::cout << "Mouse press: " << xiEvent->event_x << "," << xiEvent->event_y << std::endl;
			break;
		case XI_ButtonRelease:
            std::cout << "Mouse release: " << xiEvent->event_x << "," << xiEvent->event_y << std::endl;
			break;
		case XI_Motion:
            std::cout << "Mouse update: " << xiEvent->event_x << "," << xiEvent->event_y << std::endl;
			break;
		case XI_TouchBegin:
            std::cout << "Processing touch begin..." << std::endl;
			break;
		case XI_TouchUpdate:
            std::cout << "Processing touch update..." << std::endl;
			break;
		case XI_TouchEnd:
            std::cout << "Processing touch end..." << std::endl;
			break;
	}
    
    return false;
}

int main()
{
    std::cout << "Open display connection and check for XInput version." << std::endl;
    Display* display = XOpenDisplay(NULL);

    int opcode, event, error;
    if (!XQueryExtension(display, "XInputExtension", &opcode, &event, &error))
    {
        std::cerr << "Failed to get the XInput extension." << std::endl;

        XCloseDisplay(display);
        return -1;
    }

    int major = 2, minor = 3;
    if (XIQueryVersion(display, &major, &minor) == BadRequest)
    {
        std::cout << "Unsupported XInput extension version: expected 2.3+, actual "
            << major << "." << minor << std::endl;

        XCloseDisplay(display);
        return -1;
    }

    std::cout << "Display connection opened, with XInput version " << major << "." << minor << std::endl;

    std::cout << "Create window" << std::endl;

    int screen = DefaultScreen(display);
    Window rootWindow = XDefaultRootWindow(display);
    long black = BlackPixel(display, screen);
    long white = WhitePixel(display, screen);

    Window window = XCreateSimpleWindow(display, rootWindow, 20, 20, 640, 480, 2, white, black);
    XSetStandardProperties(display, window, "X11TouchMultiWindow", "", None, NULL, 0, NULL);
    XSelectInput(display, window, StructureNotifyMask|ExposureMask);

    // Handling close gracefully
    Atom wmDeleteMessage = XInternAtom(display, "WM_DELETE_WINDOW", False);
    XSetWMProtocols(display, window, &wmDeleteMessage, 1);
    
    std::cout << "Show window" << std::endl;

    XClearWindow(display, window);
	XMapRaised(display, window);

    // Wait for the MapNotify event
    for(;;)
    {
        XEvent e;
        XNextEvent(display, &e);
        if (e.type == MapNotify)
        {
            break;
        }
    }

    // This call actually 
    XFlush(display);

    std::cout << "Setup input handling" << std::endl;

    unsigned char mask[XIMaskLen(XI_LASTEVENT)];
	memset(mask, 0, sizeof(mask));
    // Mouse buttons
	XISetMask(mask, XI_ButtonPress);
	XISetMask(mask, XI_ButtonRelease);
    // Mouse motion
	XISetMask(mask, XI_Motion);
    // Touch
	XISetMask(mask, XI_TouchBegin);
	XISetMask(mask, XI_TouchUpdate);
	XISetMask(mask, XI_TouchEnd);

	XIEventMask eventMask = {
		.deviceid = XIAllMasterDevices, // TODO Only touch devices? Or XIAllDevices?
		.mask_len = sizeof(mask),
		.mask = mask
	};

	Status status = XISelectEvents(display, window, &eventMask, 1);
    if (status != Success)
	{
        std::cout << "Failed to select pointer events on window: " << window << std::endl;

        XDestroyWindow(display, window);
        XCloseDisplay(display);
        return -1;
    }

    std::cout << "Process event queue" << std::endl;

    bool shutdownRequested = false;
    while (!shutdownRequested)
    {
        // Ensure event buffer is flushed, and we can process the event queue
        XFlush(display);

        XEvent xEvent;
        while (XEventsQueued(display, QueuedAlready) > 0)
        {
            XNextEvent(display, &xEvent);
            switch (xEvent.type)
            {
                case Expose:
                    break;

                case ClientMessage:
                    if (xEvent.xclient.data.l[0] == wmDeleteMessage)
                    {
                        std::cout << "Shutdown requested" << std::endl;
                        shutdownRequested = true;
                    }
                    break;

                case GenericEvent:
                    {
                        if (xEvent.xcookie.extension != opcode)
                        {
                            std::cout << "Received event of type " << xEvent.type << " for window " << window << std::endl;
                            continue;
                        }

                        XGetEventData(display, &xEvent.xcookie);
                        XIDeviceEvent* xiEvent = (XIDeviceEvent*)xEvent.xcookie.data;
                        
                        Window w = xiEvent->event;
                        if (w != window)
                        {
                            w = xEvent.xany.window;
                            std::cout << "Received window from xany.window" << std::endl;
                        }

                        if (w != window)
                        {
                            std::cout << "Received event from unknown window" << std::endl;

                            XFreeEventData(display, &xEvent.xcookie);
                            continue;
                        }

                        shutdownRequested = processEvent(xiEvent);

                        XFreeEventData(display, &xEvent.xcookie);
                    }
                    break;

                default:
                    std::cout << "Received event of type " << xEvent.type << " for window " << window << std::endl;
                    break;
            }
        }
    }

    std::cout << "Clean up" << std::endl;

    XUnmapWindow(display, window);
    XDestroyWindow(display, window);
    XCloseDisplay(display);

    return 0;
}
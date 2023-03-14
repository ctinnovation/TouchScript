#include <cstring>
#include <iostream>
#include <string>
#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <X11/extensions/XInput2.h>

void processEvent(XIDeviceEvent* xiEvent)
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
            std::cout << "Touch begin:" << xiEvent->detail << ":" << xiEvent->event_x << "," << xiEvent->event_y << std::endl;
			break;
		case XI_TouchUpdate:
            std::cout << "Touch update:" << xiEvent->detail << ":" << xiEvent->event_x << "," << xiEvent->event_y << std::endl;
			break;
		case XI_TouchEnd:
            std::cout << "Touch end::" << xiEvent->detail << ":" << xiEvent->event_x << "," << xiEvent->event_y << std::endl;
			break;
	}
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
    Window rootWindow = XRootWindow(display, screen);
    long black = BlackPixel(display, screen);
    long white = WhitePixel(display, screen);

    Window window = XCreateSimpleWindow(display, rootWindow, 20, 20, 1920, 720, 2, white, black);
    XSetStandardProperties(display, window, "X11TouchMultiWindow", "", None, NULL, 0, NULL);
    XSelectInput(display, window, ExposureMask | StructureNotifyMask | XI_TouchOwnershipChangedMask);

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

    // XIEventMask eventMask = {
	// 							.deviceid = XIAllMasterDevices,
	// 							.mask_len = sizeof(mask),
	// 							.mask = mask
	// 						};

	// 						Status status = XISelectEvents(display, window, &eventMask, 1);

	int numDevices, numActualDevices;
	XIDeviceInfo* devices = XIQueryDevice(display, XIAllDevices, &numDevices);

    std::cout << "Found " << numDevices << " input devices" << std::endl;

	for (int i = 0; i < numDevices; i++)
	{
		XIDeviceInfo device = devices[i];
		if (device.use == XIMasterPointer || device.use == XISlavePointer || device.use == XIFloatingSlave)
		{
            std::cout << "Found input device " << device.name << " with " << device.num_classes << " classes" << std::endl;

			for (int j = 0; j < device.num_classes; j++)
			{
				XIAnyClassInfo* classInfo = device.classes[j];
				switch (classInfo->type)
				{
					// Touch
					case XITouchClass:
					// Mouse, touchpad
					case XIButtonClass:
					case XIValuatorClass:
						{
                            XIEventMask eventMask = {
								.deviceid = classInfo->sourceid,
								.mask_len = sizeof(mask),
								.mask = mask
							};

							Status status = XISelectEvents(display, window, &eventMask, 1);
							if (status != Success)
							{
								std::cerr << "Failed to select events for " << device.name << " on windows " << status << std::endl;
							}
						}
						break;
				}
			}
		}
	}

	// TODO XIDeviceInfo *devices = XIQueryDevice(xDisplay, XIAllDevices, &deviceCount);
	// https://www.x.org/archive/X11R7.5/doc/man/man3/XIQueryDevice.3.html

	XIFreeDeviceInfo(devices);

    XFlush(display);

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
                            //std::cout << "Received event of type " << xEvent.type << " for window " << window << std::endl;
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

                        processEvent(xiEvent);

                        XFreeEventData(display, &xEvent.xcookie);
                    }
                    break;

                default:
                    //std::cout << "Received event of type " << xEvent.type << " for window " << window << std::endl;
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
#include <iostream>
#include <string>
#include <X11/Xlib.h>

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

    int event, error;
    if (!XQueryExtension(display, "XInputExtension", &mOpcode, &event, &error))
    {
        fprintf(stderr, "Failed to get the XInput extension.");

        XCloseDisplay(display);
        return -1;
    }

    int major = 2, minor = 3;
    if (XIQueryVersion(display, &major, &minor) == BadRequest)
    {
        fprintf(stderr, "Unsupported XInput extension version: expected 2.3+, actual " +
            std::to_string(major) + "." + std::to_string(minor));

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
	free(eventMask.mask);

    if (status != Success)
	{
		fprintf(stderr, "Failed to select pointer events on window: " + std::to_string(status));

        XCloseDisplay(display);
        return -1;
    }

    bool shutdownRequested = false;
    while (!shutdownRequested)
    {
        XEvent e;
        while (XEventsQueued(display, QueuedAlready))
        {
            XNextEvent(display, &xEvent);
            if (xEvent.type != GenericEvent || xEvent.xcookie.extension != mOpcode)
            {
                // Received a non xinput event
                printf("Received event of type " + std::to_string(e.type) + " for window " + std::to_string(window));
                continue;
            }

            //XGetEventData(mDisplay, e.xcookie);
            XIDeviceEvent* xiEvent = (XIDeviceEvent*)e.xcookie.data;
            
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

            //XFreeEventData(mDisplay, e.xcookie);
        }
    }

    XCloseDisplay(display);
}

/*
#include <iostream>
#include <vector>
#include <string>
#include <X11/Xlib.h>
#include <X11/Xatom.h>

using namespace std;

void FindWindows(Display* display, Window w, Window p, Atom atomPID, int depth)
{
    Atom           type;
    int            format;
    unsigned long  nItems;
    unsigned long  bytesAfter;
    unsigned char *propPID = 0;

    char* windowName = NULL;
    XFetchName(display, w, &windowName);
    
    char* parentWindowName = NULL;
    if (p != 0)
    {
        XFetchName(display, p, &parentWindowName);
    }

    if (XGetWindowProperty(display, w, atomPID, 0, 1, False, XA_CARDINAL, &type, &format, &nItems, &bytesAfter, &propPID) == Success)
    {
        if (propPID != 0)
        {
            auto pid = *((unsigned long*)propPID);
            if (windowName != 0)
            {
                if (parentWindowName != NULL)
                {
                    cout << "Found " << pid << ": " << w << "(" << windowName << "), " << p << "(" << parentWindowName << ") " << " at " << depth << endl;
                }
                else
                {
                    cout << "Found " << pid << ": " << w << "(" << windowName << "), " << p << "(NULL) " << " at " << depth << endl;
                }
                
            }
            XFree(propPID);

            // 62914565
            // 62914569
        }
    }
    else
    {
        cout << "WARN: found unsupported window at " << depth << endl;
    }

    if (parentWindowName != NULL)
    {
        XFree(parentWindowName);
    }
    XFree(windowName);

    // Recurse into child windows
    Window rootWindow;
    Window parentWindow;
    Window* childWindows;
    unsigned numChildWindows;

    if (XQueryTree(display, w, &rootWindow, &parentWindow, &childWindows, &numChildWindows) != 0)
    {
        depth++;

        for (unsigned i = 0; i < numChildWindows; i++)
        {
            FindWindows(display, childWindows[i], parentWindow, atomPID, depth);
        }

        if (childWindows)
        {
            XFree(childWindows);
        }
    }
}

int main()
{
    Display* display = XOpenDisplay(NULL);
    Window defaultRootWindow = XDefaultRootWindow(display);

    Atom atomPID = XInternAtom(display, "_NET_WM_PID", True);
    if (atomPID == None)
    {
        cout << "No such atom" << endl;
    }
    else
    {
        cout << "Atom PID: " << atomPID << endl;
        FindWindows(display, defaultRootWindow, None, atomPID, 0);
    }

    XCloseDisplay(display);

    return 0;
}
*/
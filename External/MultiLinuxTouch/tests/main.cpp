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
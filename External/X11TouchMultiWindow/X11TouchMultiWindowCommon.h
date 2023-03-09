/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#pragma once

#define EXPORT_API __attribute__((visibility("default")))

/**	*/
typedef enum
{
	OK = 0,
	ERROR_NULL_POINTER = -101,
	ERROR_API = -102,
	ERROR_UNSUPPORTED = -103,
	ERROR_DUPLICATE_ITEM = -104
} Result;

/**	*/
typedef enum
{
	DEBUG = 0,
	INFO = 1,
	WARNING = 2,
	ERROR = 3
} MessageType;

/**	*/
typedef void(*MessageCallback)(int, char*);
/** */
typedef void(*PointerCallback)(void);

#if _UNICODE
#define CONSOLE_WRITE_LINE(message) { \
			std::wcout << message << std::endl; \
		}
#else
#define CONSOLE_WRITE_LINE(message) { \
			std::cout << message << std::endl; \
		}
#endif

class PointerHandler;
class PointerHandlerSystem;
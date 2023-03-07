/*
@author Jorrit de Vries (jorrit@jorritdevries.com)
*/
#pragma once

#define EXPORT_API __attribute__((visibility("default")))

/**	*/
typedef enum
{
	OK = 0,
	ERROR_NULL_POINTER = -101,
	ERROR_API = -102
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

#if _UNICODE
#define CONSOLE_WRITE_LINE(message) { \
			std::wcout << message << std::endl; \
		}
#else
#define CONSOLE_WRITE_LINE(message) { \
			std::cout << message << std::endl; \
		}
#endif
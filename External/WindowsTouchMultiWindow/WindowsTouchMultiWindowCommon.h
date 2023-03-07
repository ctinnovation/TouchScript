/*
* @author Jorrit de Vries (jorrit@ijsfontein.nl)
*/

#pragma once

#include <comdef.h>
#include "WindowsTouchMultiWindow.h"

#define EXPORT_API __declspec(dllexport)

/**	*/
typedef enum
{
	R_OK = 0,
	R_ERROR_NULL_POINTER = -101,
	R_ERROR_API = -102
} Result;

/**	*/
typedef enum
{
	MT_DEBUG = 0,
	MT_INFO = 1,
	MT_WARNING = 2,
	MT_ERROR = 3
} MessageType;

/**	*/
typedef void(__stdcall* PointerCallback)(int, UINT32, POINTER_INPUT_TYPE, Vector2, PointerData);
/**	*/
typedef void(__stdcall* MessageCallback)(int, char*);

#if _UNICODE
#define WINDOWS_CHECK_RESULT(result) { \
			if (result != ERROR_SUCCESS) { \
				_com_error error(result); /* Use specific visual studio function instead of FormatMessage */ \
				char tmp[256]; \
				sprintf_s(tmp, "%ls", error.ErrorMessage()); \
				throw std::exception(tmp); \
			} \
		}
#else
#define WINDOWS_CHECK_RESULT(result) { \
			if (result != ERROR_SUCCESS) { \
				_com_error error(result); /* Use specific visual studio function instead of FormatMessage */ \
				std::string errorMessage(const_cast<TCHAR*>(error.ErrorMessage())); \
				throw std::exception(errorMessage.c_str()); \
			} \
		}
#endif

#if _UNICODE
#define CONSOLE_WRITE_LINE(message) { \
			std::wcout << message << std::endl; \
		}
#else
#define CONSOLE_WRITE_LINE(message) { \
			std::cout << message << std::endl; \
		}
#endif
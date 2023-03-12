/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#pragma once

#define EXPORT_API __attribute__((visibility("default")))

/**	*/
typedef enum
{
	R_OK = 0,
	R_ERROR_NULL_POINTER = -101,
	R_ERROR_API = -102,
	R_ERROR_UNSUPPORTED = -103,
	R_ERROR_DUPLICATE_ITEM = -104
} Result;

/**	*/
typedef enum
{
	MT_DEBUG = 0,
	MT_INFO = 1,
	MT_WARNING = 2,
	MT_ERROR = 3
} MessageType;

typedef enum
{
	PE_DOWN = 1,
	PE_UPDATE = 2,
	PE_UP = 3
} PointerEvent;

typedef enum
{
	PT_NONE = 0,
	PT_MOUSE = 1,
	PT_TOUCH = 2
} PointerType;

typedef enum
{
	PF_NONE = 0x00000000,
	PF_NEW = 0x00000001,
	PF_FIRST_BUTTON = 0x00000010,
	PF_SECOND_BUTTON = 0x00000020,
	PF_THIRD_BUTTON = 0x00000040,
	PF_FOURTH_BUTTON = 0x00000080,
	PF_FIFTH_BUTTON = 0x00000100,
	PF_DOWN = 0x00010000,
	PF_UPDATE = 0x00020000,
	PF_UP = 0x00040000
} PointerFlags;

typedef enum
{
	PBCT_NONE,
	PBCT_FIRST_DOWN,
	PBCT_FIRST_UP,
	PBCT_SECOND_DOWN,
	PBCT_SECOND_UP,
	PBCT_THIRD_DOWN,
	PBCT_THIRD_UP,
	PBCT_FOURTH_DOWN,
	PBCT_FOURTH_UP,
	PBCT_FIFTH_DOWN,
	PBCT_FIFTH_UP
} PointerButtonChangeType;

struct Vector2
{
	float x, y;

	Vector2(float x, float y)
	{
		this->x = x;
		this->y = y;
	}
};

struct PointerData
{
	PointerFlags flags;
	PointerButtonChangeType changedButtons;
};

/**	*/
typedef void(*MessageCallback)(int, char*);
/** */
typedef void(*PointerCallback)(int, PointerEvent, PointerType, Vector2, PointerData);

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
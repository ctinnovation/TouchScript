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

typedef enum
{
	DOWN = 1,
	UPDATE = 2,
	UP = 3
} PointerEvent;

typedef enum
{
	MOUSE = 1,
	TOUCH = 2
} PointerType;

typedef enum
{
	NONE = 0x00000000,
	NEW = 0x00000001,
	FIRST_BUTTON = 0x00000010,
	SECOND_BUTTON = 0x00000020,
	THIRD_BUTTON = 0x00000040,
	FOURTH_BUTTON = 0x00000080,
	FIFTH_BUTTON = 0x00000100,
	DOWN = 0x00010000,
	UPDATE = 0x00020000,
	UP = 0x00040000
} PointerFlags;

typedef enum
{
	NONE,
	FIRST_DOWN,
	FIRST_UP,
	SECOND_DOWN,
	SECOND_UP,
	THIRD_DOWN,
	THIRD_UP,
	FOURTH_DOWN,
	FOURTH_UP,
	FIFTH_DOWN,
	FIFTH_UP
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
cmake_minimum_required(VERSION 3.16)
project(X11TouchMultiWindow)

set(CMAKE_CXX_STANDARD 11)

find_package(X11 REQUIRED)
if (NOT X11_FOUND)
  message(FATAL_ERROR "X11TouchMultiWindow: Failed find required X11 library (on debian/ubuntu try 'sudo apt-get install libx11-dev libxi-dev' to install)")
endif()

file(GLOB HEADER_FILES "${CMAKE_CURRENT_SOURCE_DIR}/*.h")
file(GLOB SOURCE_FILES "${CMAKE_CURRENT_SOURCE_DIR}/*.cpp")
include_directories(X11_INCLUDE_DIR)

set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../../Source/Assets/TouchScript/Modules/MultiWindow/Plugins/x64")

add_library(X11TouchMultiWindow SHARED ${HEADER_FILES} ${SOURCE_FILES})

target_link_libraries(X11TouchMultiWindow X11)
target_link_libraries(X11TouchMultiWindow Xi)
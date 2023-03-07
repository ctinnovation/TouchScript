#pragma once

#include <string>

#include "MultiWindowsTouchCommon.h"

/// @brief Sends a message to the given callback. This callback is passed from Unity.
/// @param messageCallback 
/// @param messageType 
/// @param message 
void sendMessage(MessageCallback messageCallback, MessageType messageType, const std::string& message);
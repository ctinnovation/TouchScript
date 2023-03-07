/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#pragma once

#include <string>

#include "X11TouchMultiWindowCommon.h"

/// @brief Sends a message to the given callback. This callback is passed from Unity.
/// @param messageCallback 
/// @param messageType 
/// @param message 
void sendMessage(MessageCallback messageCallback, MessageType messageType, const std::string& message);
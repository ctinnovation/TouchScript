/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;

namespace TouchScript.Examples.Checkers
{
    /// <exclude />
    public class Board : MonoBehaviour
    {
        private PinnedTransformGesture gesture;

        private void OnEnable()
        {
            gesture = GetComponent<PinnedTransformGesture>();
            gesture.Transformed += transformedHandler;
        }

        private void OnDisable()
        {
            gesture.Transformed -= transformedHandler;
        }

        private void transformedHandler(object sender, EventArgs e)
        {
            transform.localRotation *= Quaternion.AngleAxis(gesture.DeltaRotation, gesture.RotationAxis);
        }
    }
}
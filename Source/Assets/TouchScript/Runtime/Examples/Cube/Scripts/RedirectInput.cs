/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Gestures;
using TouchScript.Hit;
using TouchScript.InputSources;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.Examples.Cube
{
    /// <exclude />
    public class RedirectInput : InputSource
    {
        public int Width = 512;
        public int Height = 512;

        private MetaGesture gesture;
        private Dictionary<int, Pointer> map = new();

        public override bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            base.CancelPointer(pointer, shouldReturn);

            map.Remove(pointer.Id);
            if (shouldReturn)
            {
                if (PointerUtils.IsPointerOnTarget(pointer, transform, out var hit))
                {
                    var newPointer = PointerFactory.Create(pointer.Type, this);
                    newPointer.CopyFrom(pointer);
                    newPointer.Position = processCoords(hit.RaycastHit.textureCoord);
                    addPointer(newPointer);
                    pressPointer(newPointer);
                    map.Add(pointer.Id, newPointer);
                }
            }
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            gesture = GetComponent<MetaGesture>();
            if (gesture)
            {
                gesture.PointerPressed += pointerPressedHandler;
                gesture.PointerUpdated += pointerUpdatedHandler;
                gesture.PointerCancelled += pointerCancelledhandler;
                gesture.PointerReleased += pointerReleasedHandler;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (gesture)
            {
                gesture.PointerPressed -= pointerPressedHandler;
                gesture.PointerUpdated -= pointerUpdatedHandler;
                gesture.PointerCancelled -= pointerCancelledhandler;
                gesture.PointerReleased -= pointerReleasedHandler;
            }
        }

        private Vector2 processCoords(Vector2 value)
        {
            return new Vector2(value.x * Width, value.y * Height);
        }

        private void pointerPressedHandler(object sender, MetaGestureEventArgs metaGestureEventArgs)
        {
            var pointer = metaGestureEventArgs.Pointer;
            if (pointer.InputSource == this) return;

            var newPointer = PointerFactory.Create(pointer.Type, this);
            newPointer.CopyFrom(pointer);
            newPointer.Position = processCoords(pointer.GetPressData().RaycastHit.textureCoord);
            newPointer.Flags = pointer.Flags | Pointer.FLAG_ARTIFICIAL | Pointer.FLAG_INTERNAL;
            addPointer(newPointer);
            pressPointer(newPointer);
            map.Add(pointer.Id, newPointer);
        }

        private void pointerUpdatedHandler(object sender, MetaGestureEventArgs metaGestureEventArgs)
        {
            var pointer = metaGestureEventArgs.Pointer;

            if (pointer.InputSource == this) return;

            if (!map.TryGetValue(pointer.Id, out var newPointer)) return;
            if (!PointerUtils.IsPointerOnTarget(pointer, transform, out var hit)) return;
            newPointer.Position = processCoords(hit.RaycastHit.textureCoord);
            newPointer.Flags = pointer.Flags | Pointer.FLAG_ARTIFICIAL;
            updatePointer(newPointer);
        }

        private void pointerReleasedHandler(object sender, MetaGestureEventArgs metaGestureEventArgs)
        {
            var pointer = metaGestureEventArgs.Pointer;
            if (pointer.InputSource == this) return;

            if (!map.TryGetValue(pointer.Id, out var newPointer)) return;
            map.Remove(pointer.Id);
            releasePointer(newPointer);
            removePointer(newPointer);
        }

        private void pointerCancelledhandler(object sender, MetaGestureEventArgs metaGestureEventArgs)
        {
            var pointer = metaGestureEventArgs.Pointer;
            if (pointer.InputSource == this) return;

            if (!map.TryGetValue(pointer.Id, out var newPointer)) return;
            map.Remove(pointer.Id);
            cancelPointer(newPointer);
        }
    }
}
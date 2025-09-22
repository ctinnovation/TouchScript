/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using TouchScript.Gestures;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TouchScript.Examples.Photos
{
    /// <exclude />
    public class Container : MonoBehaviour
    {
        public int Width = 500;
        public int Height = 500;

        public void Add()
        {
            var toClone = transform.GetChild(Random.Range(0, transform.childCount));
            var clone = Instantiate(toClone.gameObject);
            clone.transform.SetParent(transform);
            clone.transform.localScale = Vector3.one;
            clone.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            clone.transform.localPosition = new Vector3(Random.Range(-Width / 2, Width / 2),
                Random.Range(-Height / 2, Height / 2), toClone.localPosition.z);
            initChild(clone.transform);
        }

        private void Start()
        {
            var count = transform.childCount;
            for (var i = 0; i < count; i++)
            {
                var child = transform.GetChild(i);
                initChild(child);
            }
        }

        private void initChild(Transform child)
        {
            var pressGesture = child.GetComponent<PressGesture>();
            if (pressGesture != null) pressGesture.Pressed += pressedHandler;
        }

        private void pressedHandler(object sender, EventArgs e)
        {
            var child = (sender as Gesture).transform;
            child.SetAsLastSibling();
        }
    }
}
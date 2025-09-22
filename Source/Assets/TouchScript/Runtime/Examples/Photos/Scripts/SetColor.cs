﻿/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TouchScript.Examples.UI
{
    /// <exclude />
    public class SetColor : MonoBehaviour
    {
        public List<Color> Colors;

        public void Set(int id)
        {
            GetComponent<Image>().color = Colors[id];
        }
    }
}
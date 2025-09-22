/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections;
using UnityEngine;

/// <exclude />
public class ShowMe : MonoBehaviour
{
    IEnumerator Start()
    {
        var canvas = GetComponent<Canvas>();
        canvas.enabled = false;
        yield return new WaitForSeconds(.5f);
        canvas.enabled = true;
    }
}
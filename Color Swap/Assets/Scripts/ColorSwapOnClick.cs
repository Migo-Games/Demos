using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColorSwapOnClick : MonoBehaviour
{
    void Update(){
        
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                hit.transform.GetComponent<IColorSwapRandomizer>()?.RandomizeColor();
            }
        }
    }
}

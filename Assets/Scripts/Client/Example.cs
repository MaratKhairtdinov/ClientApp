//Create three Sliders (Create>UI>Slider) and three Text GameObjects (Create>UI>Text). These are for manipulating the x, y, and z values of the Quaternion. The text will act as a label for each Slider, so position them appropriately.
//Attach this script to a GameObject.
//Click on the GameObject and attach each of the Sliders and Texts to the fields in the Inspector.

//This script shows how the numbers placed into the x, y, and z components of a Quaternion effect the GameObject when the w component is left at 1.
//Use the Sliders to see the effects.

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
public class Example : MonoBehaviour
{    
    //These are the Sliders that set the rotation. Remember to assign these in the Inspector    
    public float X = 0, Y = 0, Z = 0;
    //These are the Texts that output the current value of the rotations. Remember to assign these in the Inspector
    public Text m_TextX, m_TextY, m_TextZ;

    //Change the Quaternion values depending on the values of the Sliders
    private static Quaternion Change(float x, float y, float z)
    {
        //Return the new Quaternion
        return new Quaternion(x, y, z, 1);
    }

    public void OnQuaternionChanged()
    {        
        //Output the current values of x, y, and z
        m_TextX.text = " X : " + X;
        m_TextY.text = " Y : " + Y;
        m_TextZ.text = " Z : " + Z;

        //Rotate the GameObject by the new Quaternion
        transform.rotation = Change(X, Y, Z);
    }
}


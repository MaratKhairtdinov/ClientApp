using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class IFCData : MonoBehaviour
{

    public string IFCClass;
    public string STEPName;
    public string STEPId;
    public string STEPIndex;
    public string IFCLayer;

    public List<IFCPropertySet> propertySets;
    public List<IFCPropertySet> quantitySets;
}

[System.Serializable]
public class IFCPropertySet
{
    public string propSetName = "";
    public string propSetId = "";

    public List<IFCProperty> properties;
}

[System.Serializable]
public class IFCProperty
{
    public string propName = "";
    public string propValue = "";
}
using System;
using Mirror;

[System.Serializable]
public struct BreadboardComponentData
{
    // Component type
    public string type; // "wire", "led", "sevenSeg", "ic", "dipSwitch"
    
    // Wire fields
    public string startNode;
    public string endNode;
    
    // LED fields
    public string anode;
    public string cathode;
    
    // Common field
    public string color;
    
    // Seven Segment fields
    public string nodeA;
    public string nodeB;
    public string nodeC;
    public string nodeD;
    public string nodeE;
    public string nodeF;
    public string nodeG;
    public string nodeDP;
    public string nodeGnd1;
    public string nodeGnd2;
    
    // IC fields
    public string icType;
    public string pin1;
    public string pin2;
    public string pin3;
    public string pin4;
    public string pin5;
    public string pin6;
    public string pin7;
    public string pin8;
    public string pin9;
    public string pin10;
    public string pin11;
    public string pin12;
    public string pin13;
    public string pin14;
    public string pin15;
    public string pin16;

    // Dip Switch fields
    public bool isOn;
}

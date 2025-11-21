using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SegmentData
{
    public string id; // stable identifier for entryOrder
    public string name; // short label for lists
    public string text;
    public string segmentType;
    public List<string> participantIds = new();
    public List<string> participantNames = new();
}

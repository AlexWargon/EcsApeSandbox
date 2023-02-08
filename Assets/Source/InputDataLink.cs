using System;
using Wargon.Ecsape;

public class InputDataLink : ComponentLink<InputData> {
    
}
[Serializable]
public struct InputData : IComponent {
    public float horizontal;
    public float vertical;
    public bool fire;
}
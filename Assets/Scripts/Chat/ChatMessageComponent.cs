using Unity.Collections;
using Unity.Entities;

public struct ChatInputComponent : IComponentData
{
    public FixedString128Bytes  Message;
}
